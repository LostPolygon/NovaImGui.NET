using System.Diagnostics;
using CaseConverter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator.Metadata;
using OneOf;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CSharpCodeGenerationUtility;
using static MathUtility;
using static SyntaxFactory;
using static SyntaxFactoryCached;

public static class StructModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(StructModifiers));

    public static void RenameStructFieldType(
        BindingsModel model,
        string structName,
        string fieldNamePrefix,
        string newFieldTypeName,
        string newFieldName
    ) {
        EntityReferenceWrapper<Struct>? structureReference = model.Structs.FirstOrDefault(x => x.Value.Name == structName);
        if (structureReference == null)
            throw new InvalidOperationException($"Struct with name '{structName}' not found");

        Field? field =
            structureReference.Value.Children.OfType<Field>()
                .FirstOrDefault(x =>
                    x.Name.StartsWith(fieldNamePrefix, StringComparison.Ordinal)
                );
        if (field == null)
            throw new InvalidOperationException($"Field with name with prefix '{fieldNamePrefix}' not found in struct {structName}");

        if (field.Type.Value is not Struct fieldTypeStruct)
            throw new InvalidOperationException(
                $"Field with name with prefix '{fieldNamePrefix}' in struct {structName} " +
                $"has unexpected type {field.Type.GetType().FullName}"
            );

        fieldTypeStruct.Name = newFieldTypeName;
        field.BaseName = newFieldName;
    }

    public static void GenericizeStructByTypeName(
        BindingsModel model,
        string typeName,
        string newTypeName,
        string genericArgumentTypeName
    ) {
        EntityReferenceWrapper<Struct>? structReference =
            model.Structs
                .FirstOrDefault(x => x.Value.Name == typeName);

        if (structReference == null)
            throw new InvalidOperationException($"Found no struct with name {typeName}");

        EntityReferenceWrapper<Struct>? genericArgumentTypeReference =
            model.Structs
                .FirstOrDefault(x => x.Value.Name == genericArgumentTypeName);

        if (genericArgumentTypeReference == null)
            throw new InvalidOperationException($"Found no struct with name {genericArgumentTypeName}");

        GenericizeStruct(structReference, genericArgumentTypeReference.Reference, newTypeName);
    }

    public static void GenericizeStructsByFieldType(
        BindingsModel model,
        string baseTypeName,
        string genericTypeDiscriminatorFieldName,
        bool dereferencePointerType
    ) {
        EntityReferenceWrapper<Struct>[] allStructs =
            model.Structs
                .Where(x => x.Value.Name.StartsWith($"{baseTypeName}_", StringComparison.Ordinal))
                .ToArray();

        if (allStructs.Length == 0)
            throw new InvalidOperationException($"Found no structs with names starting with {baseTypeName}");

        foreach (EntityReferenceWrapper<Struct> structReference in allStructs) {
            IEntityReference genericArgumentReference =
                structReference.Value
                    .Children
                    .OfType<Field>()
                    .First(x => x.Name == genericTypeDiscriminatorFieldName)
                    .Type;

            if (dereferencePointerType && genericArgumentReference.Value is Pointer pointerType) {
                genericArgumentReference = pointerType.ValueType;
            }

            GenericizeStruct(structReference, genericArgumentReference, baseTypeName);
        }
    }

    public static Struct AddIntPtrAccessorPropertyForUnmanagedFunctionPointerFields(Struct structure) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Delegate fieldTypeDelegate)
                return [field];

            PropertyDeclarationSyntax delegateProperty =
                CreateUnmanagedDelegateAccessorPropertyForField(
                    field.BaseName,
                    field.Name,
                    FormatUnmanagedFunctionPointer(fieldTypeDelegate)
                );

            MemberDeclarationSyntaxEntity delegatePropertyEntity = new(delegateProperty, field) {
                Conditionals = [
                    new Conditional("NET6_0_OR_GREATER"),
                    ..field.Conditionals
                ]
            };

            field.Type = KnownEntities.IntPtr.AsEntityReference();
            return [delegatePropertyEntity, field];
        });
        return structure;
    }

    public static Struct AddPointerWrapperAccessorPropertyForPointerFields(Struct structure, BindingsContext context) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.IsExplodedFixedSizeArrayField ||
                field.Type.Value is not Pointer fieldTypePointer ||
                fieldTypePointer.IsVoidPointer()) // void* is replaced with IntPtr elsewhere
                return [field];

            BindingsFileCustomizations customizations =
                context.Customizations.RequireCustomizationByNamespace(structure.Namespace!);

            string fieldValueType = FormatType(fieldTypePointer.ValueType.Value);

            // Promote using the Pointer<T> property over raw pointer field
            string propertyName = field.BaseName;

            if (field.NamePrefix == null) {
                field.NamePrefix = "_";
            } else {
                if (Char.IsNumber(propertyName[^1])) {
                    propertyName += "_";
                }

                propertyName += "Pointer";
            }

            bool treatAsNonNullDefinition =
                customizations.TreatFieldsAsNonNull
                    .FirstOrDefault(x => x.Struct == structure.Name && x.Field == field.BaseName) != null;

            // If we know the field is never null, then generate a ref property instead
            PropertyDeclarationSyntax wrapperProperty;
            if (treatAsNonNullDefinition) {
                var propertyGetLambda = ArrowExpressionClause(ParseExpression($"ref *{field.Name}"));

                wrapperProperty =
                    PropertyDeclaration(
                            RefType(ParseTypeName(fieldValueType)),
                            propertyName
                        )
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .WithExpressionBody(propertyGetLambda)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                        .WithLeadingTrivia(CarriageReturnLineFeed)
                        .WithTrailingTrivia(LineFeed);
            } else {
                var propertyGetLambda = ArrowExpressionClause(ParseExpression($"new Pointer<{fieldValueType}>({field.Name})"));
                var propertySetLambda = ArrowExpressionClause(ParseExpression($"{field.Name} = value.Ptr"));

                wrapperProperty =
                    PropertyDeclaration(
                            ParseTypeName($"Pointer<{fieldValueType}>"),
                            propertyName
                        )
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithExpressionBody(propertyGetLambda)
                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                                .WithLeadingTrivia(CarriageReturnLineFeed)
                                .WithTrailingTrivia(LineFeed),
                            AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                .WithExpressionBody(propertySetLambda)
                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                                .WithTrailingTrivia(LineFeed)
                        );
            }

            MemberDeclarationSyntaxEntity intPtrPropertyEntity = new(wrapperProperty, field);
            return [intPtrPropertyEntity, field];
        });
        return structure;
    }

    public static Struct ConvertVoidPointerFieldsToIntPtr(Struct structure) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Pointer fieldTypePointer ||
                !fieldTypePointer.IsVoidPointer())
                return field;

            field.Type = KnownEntities.IntPtr.AsEntityReference();
            return field;
        });
        return structure;
    }

    public static Struct AddUtf8StringPropertyForCharFields(Struct structure) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.IsExplodedFixedSizeArrayField)
                return [field];

            bool isConstCharPointer = field.Type.Value.IsConstCharPointer();
            bool isFixedSizeCharArray = field.Type.Value is Array { ElementType.Value: Builtin { BuiltinName: "byte" } };

            // FIXME: this is ugly
            bool isFixedSizeCharArrayLikelyString =
                isFixedSizeCharArray &&
                ((string[]) ["name", "prefix", "suffix", "fmt"])
                .Any(nameSub =>
                    field.Name.Contains(nameSub, StringComparison.OrdinalIgnoreCase)
                );

            if (!isConstCharPointer && !isFixedSizeCharArrayLikelyString)
                return [field];

            string propertyTypeName = isFixedSizeCharArray ? "SpanUtf8String" : "NullTerminatedUtf8String";

            // Promote using the Span string property
            string propertyName = field.BaseName;

            if (field.NamePrefix == null) {
                field.NamePrefix = "_";
            } else {
                propertyName += "String";
            }

            string propertyGetExpression;
            if (isFixedSizeCharArray) {
                propertyGetExpression = $"new SpanUtf8String({field.BaseName}Span)";
            } else {
                string ntsConstructorArgument =
                    isFixedSizeCharArray ?
                        $"(byte*) Unsafe.AsPointer(ref {field.Name}[0])" :
                        field.Name;
                propertyGetExpression = $"new NullTerminatedUtf8String({ntsConstructorArgument})";
            }

            var propertyGetLambda = ArrowExpressionClause(ParseExpression(propertyGetExpression));

            PropertyDeclarationSyntax spanStringProperty =
                PropertyDeclaration(
                        ParseTypeName(propertyTypeName),
                        propertyName
                    )
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .WithExpressionBody(propertyGetLambda)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    .WithLeadingTrivia(CarriageReturnLineFeed)
                    .WithTrailingTrivia(LineFeed);

            MemberDeclarationSyntaxEntity spanStringPropertyEntity = new(spanStringProperty, field);
            return [spanStringPropertyEntity, field];
        });
        return structure;
    }

    public static Field PatchEnumReferencesInFieldArraySizeDeclaration(Field field, BindingsModel model) {
        if (field.Type.Value is not Array fieldTypeArray)
            return field;

        string? arrayBounds = fieldTypeArray.ArrayBounds;
        if (arrayBounds == null)
            return field;

        foreach (EntityReferenceWrapper<Enum> enumerationReference in model.Enums) {
            Enum enumeration = enumerationReference.Value;
            arrayBounds = arrayBounds.Replace($"{enumeration.Name}_", $"{enumeration.Name}.");
        }

        fieldTypeArray = fieldTypeArray with { ArrayBounds = arrayBounds };
        field.Type.Value = fieldTypeArray;

        return field;
    }

    public static Struct PatchManualMissingFieldArrayNumericBounds(Struct structure, BindingsModel model) {
        /*IReadOnlyList<Field> fields = structure.Children.OfType<Field>().ToList();

        switch (structure.Name) {
        }*/

        return structure;
    }

    public static Struct ExplodeFixedSizeArrayFieldsOfNonPrimitiveTypes(Struct structure, BindingsContext context) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Array fieldTypeArray)
                return [field];

            return ProcessFixedSizeArrayField(structure, field, fieldTypeArray, false, false, context);
        });

        return structure;
    }

    public static Struct CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes(Struct structure, BindingsContext context) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Array fieldTypeArray)
                return [field];

            return ProcessFixedSizeArrayField(structure, field, fieldTypeArray, false, true, context);
        });

        return structure;
    }

    // https://github.com/dotnet/runtime/issues/6924
    public static Struct WorkaroundDotNetRecursiveGenericStructBug(Struct structure, BindingsModel model) {
        switch (structure.Name) {
            case "ImGuiWindowTempData":
                Field? childWindowsField = structure.Children.OfType<Field>().FirstOrDefault(x => x.Name == "ChildWindows");
                if (childWindowsField == null) {
                    Log.LogWarning($"Field 'ChildWindows' not found in structure '{structure.Name}'");
                    break;
                }

                // Find first ImVector type and fudge it a bit
                Struct childWindowsFieldNewType = model.Structs.First(x => x.Value.Name == "ImVector").Value;
                childWindowsFieldNewType = childWindowsFieldNewType with {
                    GenericArguments = [
                        new Builtin(
                            "IntPtr",
                            typeof(IntPtr),
                            StorageClass.NotSpecified
                        ).AsEntityReference()
                    ]
                };
                childWindowsField.Comments = childWindowsField.Comments
                    .WithAddedAttached($"Original type: {FormatType(childWindowsField.Type.Value)}");

                childWindowsField.Type = childWindowsFieldNewType.AsEntityReference();
                break;
        }

        return structure;
    }

    public static Struct ConvertBitFields(Struct structure) {
        #pragma warning disable CS8321 // Local function is declared but never used
        bool Iteration() {
            #pragma warning restore CS8321 // Local function is declared but never used
            int startIndex = -1;
            int endIndex = -1;
            int currentBitSize = 0;
            Field[] allFields = structure.Children.OfType<Field>().ToArray();
            for (int i = 0; i < allFields.Length; i++) {
                Field field = allFields[i];
                if (startIndex == -1 && field.BitWidth != null) {
                    startIndex = i;
                } else if (startIndex != -1) {
                    if (field.BitWidth == null) {
                        endIndex = i - 1;
                        break;
                    }

                    if (field.BitWidth == 0)
                        throw new NotSupportedException($"Zero bitwidth fields are not supported ({structure.Name}.{field.Name})");

                    currentBitSize += field.BitWidth.Value;
                }
            }

            if (endIndex == -1) {
                endIndex = allFields.Length - 1;
            }

            if (startIndex == -1 || endIndex == -1)
                return false;

            Field[] oldFields = allFields[startIndex..(endIndex + 1)];
            Debug.Assert(oldFields[0].Value == null);
            Field combinedField = new() {
                BaseName = String.Join("_", oldFields.Select(x => x.Name)),
                Type = oldFields[0].Type,
                Value = null,

                Comments = new Comments().WithAddedNotes("Combined bitfield")
            };

            structure.Children.UpdateInPlace(oldFields[0], [combinedField]);
            foreach (Field oldField in oldFields) {
                structure.Children.Remove(oldField);
            }

            return true;
        }

        /*do {
            if (!Iteration())
                break;
        } while (true);*/

        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.BitWidth != null) {
                Log.LogWarning($"Found bitfield {structure.Name}.{field.Name}:{field.BitWidth} ({FormatType(field.Type.Value)})");
            }

            return field;
        });

        return structure;
    }

    public static Delegate PrettifyDelegateName(Delegate dlgt) {
        return dlgt with {
            Name = dlgt.Name.RemoveSuffix("Fn").RemoveSuffix("Func") + "Delegate"
        };
    }

    // C# doesn't support non-primitive fixed size array fields,
    // so we explode them into a field per array element
    private static IReadOnlyList<IEntity> ProcessFixedSizeArrayField(
        Struct structure,
        Field initialField,
        Array fieldTypeArray,
        bool explodePrimitiveTypes,
        bool createSpanAccessorForPrimitiveTypes,
        BindingsContext context
    ) {
        Entity arrayElementType = fieldTypeArray.ElementType.Value;
        bool isPrimitive = IsPrimitiveArrayElementType(fieldTypeArray.ElementType);
        long? arrayBoundsValue = fieldTypeArray.ArrayBoundsNumeric;

        // Nothing to do, nothing to check
        if (isPrimitive && !explodePrimitiveTypes && !createSpanAccessorForPrimitiveTypes && arrayBoundsValue == null)
            return [initialField];

        /*if (initialField.Name.Contains("BackupFloat")) {
            Console.WriteLine(5);
        }*/

        IEntityReference arrayElementTypeReference = arrayElementType.AsEntityReference();
        List<Field> fields;

        bool canExplode = !isPrimitive || explodePrimitiveTypes;
        if (canExplode) {
            arrayBoundsValue ??= ResolveNumericArrayBoundsValue(fieldTypeArray, context.NumericValueSubstitutions);
            if (arrayBoundsValue == null)
                throw new InvalidOperationException(
                    $"Failed to determine numeric array bounds for field {structure.Name}.{initialField.Name} " +
                    $"(expression {fieldTypeArray.ArrayBounds})"
                );

            fields =
                Enumerable
                    .Range(0, (int) arrayBoundsValue.Value)
                    .Select(index => {
                        Comments fieldComments = initialField.Comments;
                        if (index == 0) {
                            fieldComments = fieldComments.WithAddedNotes(
                                $"{initialField.Name} [total {arrayBoundsValue.Value} elements ({fieldTypeArray.ArrayBounds})]"
                            );
                        }

                        return initialField with {
                            BaseName = $"{initialField.Name}_{index}",
                            Type = arrayElementTypeReference,
                            Comments = fieldComments,
                            IsExplodedFixedSizeArrayField = true
                        };
                    })
                    .ToList();
        } else {
            fields = [initialField];
        }

        if (isPrimitive && !createSpanAccessorForPrimitiveTypes)
            return fields;

        string propertyName = initialField.BaseName;
        if (isPrimitive) {
            if (initialField.NamePrefix == null) {
                // Rename the base field as otherwise the Span accessor would be named the same.
                // The expectation is that the Span accessor would be the preferred way.
                initialField.NamePrefix = "_";
            } else {
                propertyName += "Span";
            }
        }

        string arrayBoundsString =
            arrayBoundsValue != null ?
                arrayBoundsValue.Value.ToString() :
                $"(int) ({fieldTypeArray.ArrayBounds})";

        // Leave the actual values to be calculated by the compiler where we can
        if (arrayBoundsValue == null) {
            initialField.Type.Value = (Array) initialField.Type.Value with {
                ArrayBounds = arrayBoundsString
            };
        }

        PropertyDeclarationSyntax spanAccessorProperty =
            CreateFixedSizeArrayFieldSpanAccessorProperty(
                propertyName,
                initialField.Name,
                arrayElementType,
                arrayBoundsString,
                !isPrimitive,
                initialField.Name
            );

        return [new MemberDeclarationSyntaxEntity(spanAccessorProperty, initialField), ..fields];
    }

    private static PropertyDeclarationSyntax CreateFixedSizeArrayFieldSpanAccessorProperty(
        string propertyName,
        string fieldName,
        Entity arrayElementType,
        string arrayBounds,
        bool asPointerToExplodedField,
        string primitiveFieldName
    ) {
        string arrayElementTypeFormatted =
            FormatType(
                arrayElementType,
                new FormatTypeOptions { UsePointerWrapper = true }
            );

        OneOf<StatementSyntax, ArrowExpressionClauseSyntax> getBody;
        if (asPointerToExplodedField) {
            string pointerVariableName = $"{fieldName.ToCamelCase()}Pointer";

            // TODO: use MemoryMarshal.CreateSpan whenever possible
            // public Span<ImGuiKeyData> KeysData => MemoryMarshal.CreateSpan(ref KeysData_0, 652);
#if false
            getBody =
                FixedStatement(
                    VariableDeclaration(
                        ParseTypeName("void*"),
                        SeparatedList<VariableDeclaratorSyntax>(
                            new SyntaxNodeOrTokenList(
                                VariableDeclarator(
                                    Identifier(pointerVariableName),
                                    default,
                                    EqualsValueClause(ParseExpression($"&{fieldName}_0"))
                                )
                            )
                        )
                    ),
                    ReturnStatement(
                        ParseExpression($"new Span<{arrayElementTypeFormatted}>({pointerVariableName}, {arrayBounds})")
                    )
                );
#else
            getBody =
                ArrowExpressionClause(
                    ParseExpression($"UnsafeUtility.CreateSpan(ref {fieldName}_0, {arrayBounds})")
                );
#endif
        } else {
            getBody =
                ArrowExpressionClause(
                    ParseExpression($"UnsafeUtility.CreateSpan(ref {fieldName}[0], {arrayBounds})")
                );
        }

        PropertyDeclarationSyntax spanAccessorProperty =
            PropertyDeclaration(
                    ParseTypeName($"Span<{arrayElementTypeFormatted}>"),
                    propertyName
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword));

        getBody.Switch(
            statementSyntax => {
                spanAccessorProperty =
                    spanAccessorProperty
                        .AddAccessorListAccessors(
                            AccessorDeclaration(
                                SyntaxKind.GetAccessorDeclaration,
                                Block(statementSyntax)
                            )
                        );
            },
            arrowSyntax =>
                spanAccessorProperty = spanAccessorProperty
                    .WithExpressionBody(arrowSyntax)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                    .WithLeadingTrivia(CarriageReturnLineFeed)
                    .WithTrailingTrivia(LineFeed)
        );

        return spanAccessorProperty;
    }

    private static bool IsPrimitiveArrayElementType(IEntityReference arrayElementTypeReference) {
        // Treat bool as non-primitive because by default,
        // it's marshalled as 4 bytes instead of 1 byte
        arrayElementTypeReference = arrayElementTypeReference.ExpandTypedef()!;
        if (arrayElementTypeReference.Value is not Builtin builtin)
            return false;

        if (builtin.ManagedType == typeof(bool))
            return false;

        return true;
    }

    private static PropertyDeclarationSyntax CreateUnmanagedDelegateAccessorPropertyForField(
        string propertyName,
        string fieldName,
        string fieldType
    ) {
        var propertyGetLambda = ArrowExpressionClause(ParseExpression($"({fieldType}) {fieldName}"));
        var propertySetLambda = ArrowExpressionClause(ParseExpression($"{fieldName} = (IntPtr) value"));

        if (Char.IsNumber(propertyName[^1])) {
            propertyName += "_";
        }

        propertyName += "Delegate";

        PropertyDeclarationSyntax unmanagedDelegateProperty =
            PropertyDeclaration(
                    ParseTypeName(fieldType),
                    propertyName
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithExpressionBody(propertyGetLambda)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                        .WithLeadingTrivia(CarriageReturnLineFeed)
                        .WithTrailingTrivia(LineFeed),
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithExpressionBody(propertySetLambda)
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                        .WithTrailingTrivia(LineFeed)
                );

        return unmanagedDelegateProperty;
    }

    private static long? ResolveNumericArrayBoundsValue(
        Array fieldTypeArray,
        ValueSubstitutions<long> valueSubstitutions
    ) {
        // We have a numeric literal, we're golden
        long? arrayBoundsValue = fieldTypeArray.ArrayBoundsNumeric;
        if (arrayBoundsValue != null)
            return arrayBoundsValue;

        string? arrayBounds = fieldTypeArray.ArrayBounds;
        if (arrayBounds == null) {
            Log.LogWarning($"Field with type {fieldTypeArray.ElementType} has neither ArrayBoundsNumeric or ArrayBounds");
            return null;
        }

        arrayBounds = valueSubstitutions.Substitute(arrayBounds, out (bool found, long value) exactMatch);
        if (exactMatch.found)
            return exactMatch.value;

        arrayBoundsValue = EvaluateExpressionAsLong(arrayBounds);
        if (arrayBoundsValue != null)
            return arrayBoundsValue;

        return arrayBoundsValue;
    }

    private static void GenericizeStruct(
        EntityReferenceWrapper<Struct> structReference,
        IEntityReference genericArgumentReference,
        string newTypeName
    ) {
        structReference.Reference.Value = structReference.Value with {
            Name = newTypeName,
            GenericArguments = [genericArgumentReference],
            NoEmit = true
        };
    }
}

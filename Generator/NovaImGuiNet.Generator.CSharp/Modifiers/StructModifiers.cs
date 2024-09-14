using System.Diagnostics;
using CaseConverter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator.Metadata;

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

            // FIXME: move IntPtr field generation from outside BindingsModelCodeGenerator
            // to a modifier
            PropertyDeclarationSyntax intPtrProperty =
                CreateIntPtrAccessorPropertyForField(
                    field.BaseName,
                    field.Name,
                    FormatUnmanagedFunctionPointer(fieldTypeDelegate)
                );

            PropertyDeclarationSyntax intPtrLegacyProperty =
                CreateIntPtrAccessorPropertyForField(
                    field.BaseName,
                    field.Name,
                    "IntPtr"
                );

            MemberDeclarationSyntaxEntity intPtrPropertyEntity =
                new(intPtrProperty, field) {
                    Conditionals = [
                        ..field.Conditionals,
                        new Conditional("!NETSTANDARD2_0_OR_GREATER")
                    ]
                };
            MemberDeclarationSyntaxEntity intPtrLegacyPropertyEntity =
                new(intPtrLegacyProperty, field) {
                    Conditionals = [
                        ..field.Conditionals,
                        new Conditional("NETSTANDARD2_0_OR_GREATER")
                    ]
                };

            return [intPtrPropertyEntity, intPtrLegacyPropertyEntity, field];
        });
        return structure;
    }

    public static Struct AddIntPtrAccessorPropertyForPointerFields(Struct structure) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Pointer fieldTypePointer ||
                fieldTypePointer.IsVoidPointer()) // void* is replaced with IntPtr
                return [field];

            PropertyDeclarationSyntax intPtrProperty =
                CreateIntPtrAccessorPropertyForField(
                    field.BaseName,
                    field.Name,
                    FormatType(fieldTypePointer)
                );

            MemberDeclarationSyntaxEntity intPtrPropertyEntity = new(intPtrProperty, field);
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

    public static Struct AddNullTerminatedStringPropertyForConstCharPointerFields(Struct structure) {
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

            // Promote using the NTS property
            string propertyName = field.BaseName;

            if (field.NamePrefix == null) {
                field.NamePrefix = "_";
            } else {
                propertyName += "String";
            }

            string ntsConstructorArgument =
                isFixedSizeCharArray ?
                    $"(byte*) Unsafe.AsPointer(ref {field.Name}[0])" :
                    field.Name;

            var propertyGetLambda = ArrowExpressionClause(
                ParseExpression($"new NullTerminatedString({ntsConstructorArgument})")
            );

            PropertyDeclarationSyntax ntsProperty =
                PropertyDeclaration(
                        ParseTypeName("NullTerminatedString"),
                        propertyName
                    )
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithExpressionBody(propertyGetLambda)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                            .WithLeadingTrivia(CarriageReturnLineFeed)
                            .WithTrailingTrivia(LineFeed)
                    );

            MemberDeclarationSyntaxEntity ntsPropertyEntity = new(ntsProperty, field);
            return [ntsPropertyEntity, field];
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

    public static Struct FixMissingFieldArrayNumericBounds(
        Struct structure,
        MetadataDto dto,
        BindingsContext context,
        BindingsModel model
    ) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Array fieldTypeArray)
                return field;

            return FixMissingFieldArrayNumericBounds(
                field,
                fieldTypeArray,
                context.Options.CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes,
                dto,
                model
            );
        });

        return structure;
    }

    // FIXME: can at least some of those be automated?
    public static Struct PatchManualMissingFieldArrayNumericBounds(Struct structure, BindingsModel model) {
        IReadOnlyList<Field> fields = structure.Children.OfType<Field>().ToList();

        switch (structure.Name) {
            case "ImFont": {
                Field used4kPagesMapField = fields.First(f => f.Name == "Used4kPagesMap");
                Array fieldArrayType = (Array) used4kPagesMapField.Type.Value;

                // ReSharper disable once InconsistentNaming
#if IMGUI_USE_WCHAR32
                const int IM_UNICODE_CODEPOINT_MAX = 0x10FFFF;
#else
                const int IM_UNICODE_CODEPOINT_MAX = 0xFFFF;
#endif
                fieldArrayType = fieldArrayType with {
                    ArrayBoundsNumeric = (IM_UNICODE_CODEPOINT_MAX + 1) / 4096 / 8
                };
                used4kPagesMapField.Type.Value = fieldArrayType;
                break;
            }
            case "ImFontAtlas": {
                Field texUvLinesField = fields.First(f => f.Name == "TexUvLines");
                Array fieldArrayType = (Array) texUvLinesField.Type.Value;

                // ReSharper disable once InconsistentNaming
                const uint IM_DRAWLIST_TEX_LINES_WIDTH_MAX = 63;
                fieldArrayType = fieldArrayType with {
                    ArrayBoundsNumeric = IM_DRAWLIST_TEX_LINES_WIDTH_MAX + 1
                };
                texUvLinesField.Type.Value = fieldArrayType;
                break;
            }
            case "ImGuiStyle": {
                Field colorsField = fields.First(f => f.Name == "Colors");
                Array fieldArrayType = (Array) colorsField.Type.Value;

                Enum.Member imgGuiColCount =
                    model.Enums
                        .Select(e => e.Value)
                        .First(e => e.Name == "ImGuiCol")
                        .Members
                        .First(m => m.Name == "COUNT");
                fieldArrayType = fieldArrayType with {
                    ArrayBoundsNumeric = imgGuiColCount.ValueNumeric
                };
                colorsField.Type.Value = fieldArrayType;
                break;
            }
            case "ImGuiIO": {
                // When ImGuiKey.KeysData_SIZE = COUNT
                Field keysDataCountFieldOld = fields.First(f => f.Name == "KeysData");
                Field keysDataCountField = keysDataCountFieldOld with { };

                Array keysDataCountFieldArrayType = (Array) keysDataCountField.Type.Value;

                Enum.Member imgGuiColCount =
                    model.Enums
                        .Select(e => e.Value)
                        .First(e => e.Name == "ImGuiKey")
                        .Members
                        .First(m => m.Name == "COUNT");
                keysDataCountFieldArrayType = keysDataCountFieldArrayType with {
                    ArrayBoundsNumeric = imgGuiColCount.ValueNumeric
                };
                keysDataCountField = keysDataCountField with {
                    Type = keysDataCountFieldArrayType.AsEntityReference(),
                    Conditionals = [
                        ..keysDataCountField.Conditionals,
                        new("ifdef", "!IMGUI_DISABLE_OBSOLETE_KEYIO")
                    ]
                };

                // When ImGuiKey.KeysData_SIZE = NamedKey_COUNT
                Field keysDataNamedKeyCountField = keysDataCountFieldOld with { };

                Array keysDataNamedKeyCountFieldArrayType = (Array) keysDataNamedKeyCountField.Type.Value;

                Enum.Member imgGuiColNamedKeyCount =
                    model.Enums
                        .Select(e => e.Value)
                        .First(e => e.Name == "ImGuiKey")
                        .Members
                        .First(m => m.Name == "NamedKey_COUNT");
                keysDataNamedKeyCountFieldArrayType = keysDataNamedKeyCountFieldArrayType with {
                    ArrayBoundsNumeric = imgGuiColNamedKeyCount.ValueNumeric
                };
                keysDataNamedKeyCountField.Type.Value = keysDataNamedKeyCountFieldArrayType;
                keysDataNamedKeyCountField = keysDataNamedKeyCountField with {
                    Type = keysDataNamedKeyCountFieldArrayType.AsEntityReference(),
                    Conditionals = [
                        ..keysDataNamedKeyCountField.Conditionals,
                        new("ifdef", "IMGUI_DISABLE_OBSOLETE_KEYIO")
                    ]
                };

                structure.Children.UpdateInPlace(keysDataCountFieldOld, [keysDataCountField, keysDataNamedKeyCountField]);
                break;
            }
        }

        return structure;
    }

    public static Struct ExplodeFixedSizeArrayFieldsOfNonPrimitiveTypes(Struct structure) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Array fieldTypeArray)
                return [field];

            return ProcessFixedSizeArrayField(structure, field, fieldTypeArray, false, false);
        });

        return structure;
    }

    public static Struct CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes(Struct structure) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Array fieldTypeArray)
                return [field];

            return ProcessFixedSizeArrayField(structure, field, fieldTypeArray, false, true);
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

                // Find first found ImVector type and fudge it a bit
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

    public static Delegate PrettifyDelegateName(Delegate @delegate) {
        @delegate.Name = @delegate.Name.RemoveSuffix("Fn") + "Delegate";
        return @delegate;
    }

    private static Field FixMissingFieldArrayNumericBounds(
        Field field,
        Array fieldTypeArray,
        bool resolveNumericArrayBoundsForPrimitiveTypes,
        MetadataDto dto,
        BindingsModel model
    ) {
        string? arrayBounds = fieldTypeArray.ArrayBounds;
        long? arrayBoundsNumeric =
            fieldTypeArray.ArrayBoundsNumeric ??
            EvaluateExpressionAsLong(fieldTypeArray.ArrayBounds);

        if (arrayBoundsNumeric == null &&
            (!IsPrimitiveArrayElementType(fieldTypeArray.ElementType) ||
                resolveNumericArrayBoundsForPrimitiveTypes)
           ) {
            arrayBoundsNumeric = ResolveNumericArrayBoundsValue(fieldTypeArray, dto, model);
        }

        if (arrayBoundsNumeric == null) {
            // This cast is needed when enum values are used as array bounds
            arrayBounds = $"(int) ({fieldTypeArray.ArrayBounds})";
        }

        fieldTypeArray = fieldTypeArray with {
            ArrayBounds = arrayBounds,
            ArrayBoundsNumeric = arrayBoundsNumeric
        };

        field = field with {
            Type = fieldTypeArray.AsEntityReference()
        };

        return field;
    }

    // C# doesn't support non-primitive fixed size array fields,
    // so we explode them into a field per array element
    private static IReadOnlyList<IEntity> ProcessFixedSizeArrayField(
        Struct structure,
        Field field,
        Array fieldTypeArray,
        bool explodePrimitiveTypes,
        bool createSpanAccessorForPrimitiveTypes
    ) {
        Entity arrayElementType = fieldTypeArray.ElementType.Value;
        bool isPrimitive = IsPrimitiveArrayElementType(fieldTypeArray.ElementType);
        long? arrayBoundsValue = fieldTypeArray.ArrayBoundsNumeric;

        // Nothing to do, nothing to check
        if (isPrimitive && !explodePrimitiveTypes && !createSpanAccessorForPrimitiveTypes && arrayBoundsValue == null)
            return [field];

        if (arrayBoundsValue == null)
            throw new Exception(
                $"Failed to determine numeric array bounds for field {structure.Name}.{field.Name} " +
                $"(expression {fieldTypeArray.ArrayBounds})"
            );

        IEntityReference arrayElementTypeReference = arrayElementType.AsEntityReference();
        List<Field> fields;
        if (!isPrimitive || explodePrimitiveTypes) {
            fields =
                Enumerable
                    .Range(0, (int) arrayBoundsValue.Value)
                    .Select(index => {
                        Comments fieldComments = field.Comments;
                        if (index == 0) {
                            fieldComments = fieldComments.WithAddedNotes(
                                $"{field.Name} [total {arrayBoundsValue.Value} elements ({fieldTypeArray.ArrayBounds})]"
                            );
                        }

                        return field with {
                            BaseName = $"{field.Name}_{index}",
                            Type = arrayElementTypeReference,
                            Comments = fieldComments,
                            IsExplodedFixedSizeArrayField = true
                        };
                    })
                    .ToList();
        } else {
            fields = [field];
        }

        if (isPrimitive && !createSpanAccessorForPrimitiveTypes)
            return fields;

        string propertyName = field.BaseName;
        if (isPrimitive) {
            if (field.NamePrefix == null) {
                // Rename the base field as otherwise the Span accessor would be named the same.
                // The expectation is that the Span accessor would be the preferred way.
                field.NamePrefix = "_";
            } else {
                propertyName += "Span";
            }
        }

        PropertyDeclarationSyntax spanAccessorProperty =
            CreateFixedSizeArrayFieldSpanAccessorProperty(
                propertyName,
                field.Name,
                arrayElementType,
                arrayBoundsValue.Value,
                !isPrimitive,
                field.Name
            );

        return [new MemberDeclarationSyntaxEntity(spanAccessorProperty, field), ..fields];
    }

    private static PropertyDeclarationSyntax CreateFixedSizeArrayFieldSpanAccessorProperty(
        string propertyName,
        string fieldName,
        Entity arrayElementType,
        long arrayBoundsValue,
        bool asPointerToExplodedField,
        string primitiveFieldName
    ) {
        string arrayElementTypeFormatted =
            FormatType(
                arrayElementType,
                new FormatTypeOptions { UsePointerWrapper = true }
            );

        string pointerVariableName = $"{fieldName.ToCamelCase()}Pointer";

        StatementSyntax getBody;
        if (asPointerToExplodedField) {
            // TODO: use MemoryMarshal.CreateSpan whenever possible
            // public Span<ImGuiKeyData> KeysData => MemoryMarshal.CreateSpan(ref KeysData_0, 652);
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
                        ParseExpression($"new Span<{arrayElementTypeFormatted}>({pointerVariableName}, {arrayBoundsValue})")
                    )
                );
        } else {
            getBody =
                FixedStatement(
                    VariableDeclaration(
                        ParseTypeName("void*"),
                        SeparatedList<VariableDeclaratorSyntax>(
                            new SyntaxNodeOrTokenList(
                                VariableDeclarator(
                                    Identifier(pointerVariableName),
                                    default,
                                    EqualsValueClause(ParseExpression(primitiveFieldName))
                                )
                            )
                        )
                    ),
                    ReturnStatement(
                        ParseExpression($"new Span<{arrayElementTypeFormatted}>({pointerVariableName}, {arrayBoundsValue})")
                    )
                );
        }

        PropertyDeclarationSyntax spanAccessorProperty =
            PropertyDeclaration(
                    ParseTypeName($"Span<{arrayElementTypeFormatted}>"),
                    propertyName
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    AccessorDeclaration(
                        SyntaxKind.GetAccessorDeclaration,
                        Block(getBody)
                    )
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

    private static PropertyDeclarationSyntax CreateIntPtrAccessorPropertyForField(
        string propertyName,
        string fieldName,
        string fieldType
    ) {
        var propertyGetLambda = ArrowExpressionClause(
            ParseExpression($"(IntPtr) {fieldName}")
        );

        var propertySetLambda = ArrowExpressionClause(
            ParseExpression($"{fieldName} = ({fieldType}) value")
        );

        if (Char.IsNumber(propertyName[^1])) {
            propertyName += "_";
        }

        propertyName += "IntPtr";

        PropertyDeclarationSyntax intPtrProperty =
            PropertyDeclaration(
                    ParseTypeName("IntPtr"),
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

        return intPtrProperty;
    }

    private static long? ResolveNumericArrayBoundsValue(Array fieldTypeArray, MetadataDto dto, BindingsModel model) {
        // We have a numeric literal, we're golden
        long? arrayBoundsValue = fieldTypeArray.ArrayBoundsNumeric;
        if (arrayBoundsValue != null)
            return arrayBoundsValue;

        string? arrayBounds = fieldTypeArray.ArrayBounds;
        if (arrayBounds == null) {
            Log.LogWarning($"Field with type {fieldTypeArray.ElementType} has neither ArrayBoundsNumeric or ArrayBounds");
            return null;
        }

        // Check if there's a define with the same name as the array bounds expression,
        // and if there is one, evaluate it to a number
        MetadataDto.Define? valueDefine =
            dto.Defines
                .FirstOrDefault(define => define.Name == arrayBounds);

        if (valueDefine != null) {
            arrayBoundsValue = EvaluateExpressionAsLong(valueDefine.Content);
            if (arrayBoundsValue != null)
                return arrayBoundsValue;
        }

        // Check if there's an enum member matching the array bounds expression
        foreach (Enum enumeration in model.Enums.Select(x => x.Value)) {
            foreach (Enum.Member member in enumeration.Members) {
                string fullName = $"{enumeration.Name}.{member.Name}";
                if (arrayBounds != fullName)
                    continue;

                arrayBoundsValue = member.ValueNumeric ?? EvaluateExpressionAsLong(member.Value);
                goto enumFound;
            }
        }

        enumFound:
        if (arrayBoundsValue != null)
            return arrayBoundsValue;

        // Evaluate the array bounds expression to a number
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
        Struct structure = structReference.Value;

        structure = structure with {
            Name = newTypeName,
            GenericArguments = [genericArgumentReference],
            NoEmit = true
        };

        structReference.Reference.Value = structure;
    }
}

using System.Diagnostics;
using CaseConverter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static SyntaxFactory;
using static CSharpCodeGenerationUtility;
using static MathUtility;

public static class StructModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(StructModifiers));

    public static BindingsModel RenameStructFieldType(
        BindingsModel model,
        string structName,
        string fieldNamePrefix,
        string newFieldTypeName,
        string newFieldName
    ) {
        IEntityReference<Struct>? structureReference = model.Structs.FirstOrDefault(x => x.Value.Name == structName);
        if (structureReference == null)
            throw new InvalidOperationException($"Struct with name '{structName}' not found");

        Field? field =
            structureReference.Value.Children.OfType<Field>()
                .FirstOrDefault(x =>
                    x.Name.StartsWith(fieldNamePrefix, StringComparison.Ordinal)
                );
        if (field == null)
            throw new InvalidOperationException($"Field with name with prefix '{fieldNamePrefix}' not found in struct {structName}");

        if (field.Type is not IEntityReference<Struct> fieldTypeReference)
            throw new InvalidOperationException(
                $"Field with name with prefix '{fieldNamePrefix}' in struct {structName} " +
                $"has unexpected type {field.Type.GetType().FullName}"
            );

        fieldTypeReference.Value.Name = newFieldTypeName;
        field.Name = newFieldName;

        return model;
    }

    public static BindingsModel GenericizeStructByTypeName(BindingsModel model, string typeName, string newTypeName, string genericArgumentTypeName) {
        IEntityReference<Struct>? structReference =
            model.Structs
                .FirstOrDefault(x => x.Value.Name == typeName);

        if (structReference == null)
            throw new InvalidOperationException($"Found no struct with name {typeName}");

        IEntityReference<Struct>? genericArgumentTypeReference =
            model.Structs
                .FirstOrDefault(x => x.Value.Name == genericArgumentTypeName);

        if (genericArgumentTypeReference == null)
            throw new InvalidOperationException($"Found no struct with name {genericArgumentTypeName}");

        GenericizeStruct(newTypeName, structReference, genericArgumentTypeReference);
        return model;
    }

    public static BindingsModel GenericizeStructsByFieldType(
        BindingsModel model,
        string baseTypeName,
        string genericTypeDiscriminatorFieldName,
        bool dereferencePointerType
    ) {
        IEntityReference<Struct>[] allStructs =
            model.Structs
                .Where(x => x.Value.Name.StartsWith($"{baseTypeName}_", StringComparison.Ordinal))
                .ToArray();

        if (allStructs.Length == 0)
            throw new InvalidOperationException($"Found no structs with names starting with {baseTypeName}");

        foreach (IEntityReference<Struct> structReference in allStructs) {
            IEntityReference genericArgumentReference =
                structReference.Value
                    .Children
                    .OfType<Field>()
                    .First(x => x.Name == genericTypeDiscriminatorFieldName)
                    .Type;

            if (dereferencePointerType && genericArgumentReference is IEntityReference<Pointer> pointerTypeReference) {
                genericArgumentReference = pointerTypeReference.Value.ValueType;
            }

            GenericizeStruct(baseTypeName, structReference, genericArgumentReference);
        }

        return model;
    }

    public static Struct AddUnmanagedFunctionPointerFieldRawPtrAccessor(Struct structure) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Delegate fieldTypeDelegate)
                return [field];

            return AddUnmanagedFunctionPointerFieldRawPtrAccessor(field, fieldTypeDelegate);
        });
        return structure;
    }

    public static Field PatchEnumReferencesInFieldArraySizeDeclaration(BindingsModel model, Field field) {
        if (field.Type.Value is not Array fieldTypeArray)
            return field;

        string? arrayBounds = fieldTypeArray.ArrayBounds;
        if (arrayBounds == null)
            return field;

        foreach (IEntityReference<Enum> enumerationReference in model.Enums) {
            Enum enumeration = enumerationReference.Value;
            arrayBounds = arrayBounds.Replace($"{enumeration.Name}_", $"{enumeration.Name}.");
        }

        fieldTypeArray = fieldTypeArray with { ArrayBounds = arrayBounds };
        field.Type.Value = fieldTypeArray;

        return field;
    }

    private static Field FixMissingFieldArrayNumericBounds(
        MetadataDto dto,
        BindingsModel model,
        Field field,
        Array fieldTypeArray,
        bool resolveNumericArrayBoundsForPrimitiveTypes
    ) {
        string? arrayBounds = fieldTypeArray.ArrayBounds;
        long? arrayBoundsNumeric =
            fieldTypeArray.ArrayBoundsNumeric ??
            EvaluateExpressionAsLong(fieldTypeArray.ArrayBounds);

        if (arrayBoundsNumeric == null &&
            (!IsPrimitiveArrayElementType(fieldTypeArray.ElementType.Value) ||
            resolveNumericArrayBoundsForPrimitiveTypes)
           ) {
            arrayBoundsNumeric = ResolveNumericArrayBoundsValue(dto, model, fieldTypeArray);
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

    public static Struct FixMissingFieldArrayNumericBounds(
        BindingsContext context,
        MetadataDto dto,
        BindingsModel model,
        Struct structure
    ) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Array fieldTypeArray)
                return field;

            return FixMissingFieldArrayNumericBounds(
                dto,
                model,
                field,
                fieldTypeArray,
                context.Options.CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes
            );
        });
        return structure;
    }

    // FIXME: can at least some of those be automated?
    public static Struct PatchManualMissingFieldArrayNumericBounds(BindingsModel model, Struct structure) {
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
                    ArrayBoundsNumeric = (int) ((IM_UNICODE_CODEPOINT_MAX + 1) / 4096 / 8)
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
                    Conditionals = new EqualityList<Conditional>(keysDataCountField.Conditionals) {
                        new("ifdef", "!IMGUI_DISABLE_OBSOLETE_KEYIO")
                    }
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
                    Conditionals = new EqualityList<Conditional>(keysDataNamedKeyCountField.Conditionals) {
                        new("ifdef", "IMGUI_DISABLE_OBSOLETE_KEYIO")
                    }
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

            return HandleFixedSizeArrayField(structure, field, fieldTypeArray, false, false);
        });

        return structure;
    }

    public static Struct CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes(Struct structure) {
        structure.Children.UpdateInPlace<IEntity, Field>(field => {
            if (field.Type.Value is not Array fieldTypeArray)
                return [field];

            return HandleFixedSizeArrayField(structure, field, fieldTypeArray, false, true);
        });

        return structure;
    }

    public static Struct ConvertBitFields(Struct structure) {
        bool Iteration() {
            int startIndex = -1;
            int endIndex = -1;
            int currentBitSize = 0;
            Field[] allFields = structure.Children.OfType<Field>().ToArray();
            for (int i = 0; i < allFields.Length; i++) {
                Field field = allFields[i];
                if (startIndex == -1 && field.Width != null) {
                    startIndex = i;
                } else if (startIndex != -1) {
                    if (field.Width == null) {
                        endIndex = i - 1;
                        break;
                    }

                    if (field.Width == 0)
                        throw new NotSupportedException($"Zero bitwidth fields are not supported ({structure.Name}.{field.Name})");

                    currentBitSize += field.Width.Value;
                }
            }

            if (endIndex == -1) {
                endIndex = allFields.Length - 1;
            }

            if (startIndex == -1 || endIndex == -1)
                return false;

            Field[] oldFields = allFields[startIndex..(endIndex + 1)];
            Debug.Assert(oldFields[0].Value == null);
            Field combinedField = new Field() {
                Name = String.Join("_", oldFields.Select(x => x.Name)),
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
            if (field.Width != null) {
                Log.LogWarning($"Found bitfield {structure.Name}.{field.Name}:{field.Width} ({FormatType(field.Type.Value)})");
            }

            return field;
        });

        return structure;
    }

    public static Delegate PrettifyDelegateName(Delegate @delegate) {
        @delegate.Name = @delegate.Name.RemoveFromEnd("Fn") + "Delegate";
        return @delegate;
    }

    // C# doesn't support non-primitive fixed size array fields,
    // so we explode them into a field per array element
    private static IReadOnlyList<IEntity> HandleFixedSizeArrayField(
        Struct structure,
        Field field,
        Array fieldTypeArray,
        bool explodePrimitiveTypes,
        bool createSpanAccessorForPrimitiveTypes
    ) {
        Entity arrayElementType = fieldTypeArray.ElementType.Value;
        bool isPrimitive = IsPrimitiveArrayElementType(arrayElementType);
        long? arrayBoundsValue = fieldTypeArray.ArrayBoundsNumeric;

        // Nothing to do, nothing to check
        if (isPrimitive && !explodePrimitiveTypes && !createSpanAccessorForPrimitiveTypes && arrayBoundsValue == null)
            return [field];

        if (arrayBoundsValue == null)
            throw new Exception(
                $"Failed to determine numeric array bounds for field {structure.Name}.{field.Name} " +
                $"(expression {fieldTypeArray.ArrayBounds})");

        IEntityReference arrayElementTypeReference = arrayElementType.AsEntityReference();
        List<Field> fields;
        if (!isPrimitive || explodePrimitiveTypes) {
            fields = Enumerable.Range(0, (int) arrayBoundsValue.Value)
                .Select(index => {
                    Comments fieldComments = new(
                        field.Comments.Attached,
                        field.Comments.Attached,
                        new EqualityList<string>(field.Comments.Notes)
                    );
                    fieldComments.WithAddedNotes(
                        $"{field.Name} [{index} / {(int) arrayBoundsValue.Value} ({fieldTypeArray.ArrayBounds})]"
                    );
                    return field with {
                        Name = $"{field.Name}_{index}",
                        Type = arrayElementTypeReference,
                        Comments = fieldComments
                    };
                })
                .ToList();
        } else {
            fields = [field];
        }

        if (isPrimitive && !createSpanAccessorForPrimitiveTypes)
            return fields;

        string primitiveFieldName = $"{field.Name}_";
        PropertyDeclarationSyntax spanAccessorProperty =
            CreateSpanAccessorProperty(
                field,
                arrayElementType,
                arrayBoundsValue.Value,
                !isPrimitive,
                primitiveFieldName
            );

        if (isPrimitive) {
            // Rename the base field as otherwise the Span accessor would be named the same.
            // The expectation is that the Span accessor would be the preferred way.
            field.Name = primitiveFieldName;
        }

        return [new MemberDeclarationSyntaxEntity(spanAccessorProperty, field), ..fields];
    }

    private static PropertyDeclarationSyntax CreateSpanAccessorProperty(
        Field field,
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

        string pointerVariableName = $"{field.Name.ToCamelCase()}Pointer";

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
                                    EqualsValueClause(ParseExpression($"&{field.Name}_0"))
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
                    field.Name
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

    private static bool IsPrimitiveArrayElementType(Entity arrayElement) {
        return arrayElement is Builtin or Typedef { Type: IEntityReference<Builtin> };
    }

    private static IReadOnlyList<IEntity> AddUnmanagedFunctionPointerFieldRawPtrAccessor(Field field, Delegate userDelegate) {
        var propertyGetLambda = ArrowExpressionClause(
            ParseExpression($"(IntPtr) {field.Name}")
        );
        var propertySetLambda = ArrowExpressionClause(
            ParseExpression(
                $"{field.Name} = ({FormatUnmanagedFunctionPointer(userDelegate)}) value"
            )
        );

        PropertyDeclarationSyntax property =
            PropertyDeclaration(
                    ParseTypeName("IntPtr"),
                    field.Name + "Ptr"
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

        MemberDeclarationSyntaxEntity entity = new(property, field);
        entity.Conditionals = [
            ..entity.Conditionals,
            new Conditional("if", "!NETSTANDARD2_0_OR_GREATER")
        ];

        return [entity, field];
    }

    private static long? ResolveNumericArrayBoundsValue(MetadataDto dto, BindingsModel model, Array fieldTypeArray) {
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
        string newTypeName,
        IEntityReference<Struct> structReference,
        IEntityReference genericArgumentReference
    ) {
        Struct structure = structReference.Value;

        structure = structure with {
            Name = newTypeName,
            GenericArguments = [genericArgumentReference],
            SkipEmit = true
        };

        structReference.Value = structure;
    }
}

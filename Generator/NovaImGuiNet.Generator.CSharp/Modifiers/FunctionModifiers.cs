using System.Diagnostics;
using CaseConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CSharpCodeGenerationUtility;
using static SyntaxFactory;

public static class FunctionModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(FunctionModifiers));

    public static void PatchArgumentDefaultValues<T>(BindingsModel model, T functionLike) where T : IFunctionLike {
        int? lastArgumentWithUnusableDeclarationDefaultValueIndex = null;
        for (int index = 0; index < functionLike.Arguments.Count; index++) {
            FunctionArgument argument = functionLike.Arguments[index];
            if (argument.DeclarationDefaultValue == null)
                continue;

            bool fullyFixed = PatchArgumentDefaultValue(model, functionLike, argument);
            if (!fullyFixed) {
                lastArgumentWithUnusableDeclarationDefaultValueIndex = index;
            }
        }

        if (lastArgumentWithUnusableDeclarationDefaultValueIndex == null)
            return;

        for (int index = 0; index <= lastArgumentWithUnusableDeclarationDefaultValueIndex; index++) {
            FunctionArgument argument = functionLike.Arguments[index];
            if (argument.DeclarationDefaultValue == null)
                continue;

            argument.DeclarationDefaultValue = null;
        }
    }

    public static string FixInvalidArgumentNames(string argumentName, int argumentIndex) {
        argumentName =
            String.IsNullOrEmpty(argumentName) ?
                $"arg{argumentIndex + 1}" :
                FixInvalidIdentifier(argumentName);

        return argumentName;
    }

    public static FunctionArgument PrettifyArgumentName(IFunctionLike functionLike, FunctionArgument argument) {
        if (!argument.Name.StartsWith("__")) {
            argument.Name = argument.Name.SnakeCaseToCamelCase();
        }

        return argument;
    }

    // FIXME: temporary hack for fun
    public static FunctionArgument ReplaceConstCharPointerWithString(FunctionArgument argument) {
        bool isConstCharPointer =
            argument.Type.ExpandTypedef()!.Value is Pointer { ValueType.Value: Builtin { IsConstCharPointer: true } };

        if (!isConstCharPointer)
            return argument;

        argument.Type = new Builtin(
            "string",
            typeof(string),
            StorageClass.Const
        ).AsEntityReference();

        return argument;
    }

    // TODO: merge with one below
    public static FunctionArgument PatchArrayArgumentsToUseByReference(FunctionArgument argument) {
        if (argument.Type.Value is not Array array)
            return argument;

        //if (argument.ArrayBounds == null)
        //    return argument;

        if (array.ArrayBounds == null) {
            // 1. Convert boundless arrays to pointer
            argument = argument with {
                Type = new Pointer(array.ElementType, false).AsEntityReference()
            };
        } else {
            // 2. in C, passing fixed size array to a function is actually still passing a pointer.
            // So let's make it actually pass a pointer.
            argument = argument with {
                Type = new Pointer(array.ElementType, false).AsEntityReference()
            };
        }

        return argument;
    }

    /// <summary>
    /// C function arguments like float[2] look like they are passed by value, but are actually pointers to an array.
    /// Fixed size arrays can't be represented as function arguments in C#,
    /// so we replace them by fixed-size ImFixedArray&lt;T&gt; structures.
    /// </summary>
    public static FunctionArgument ConvertFixedSizeArrayArgumentToFixedSizeStruct(BindingsContext context, FunctionArgument argument) {
        if (argument.Type.Value is not Array argumentTypeArray || String.IsNullOrEmpty(argument.ArrayBounds))
            return argument;

        argument.Type.Value =
            new Pointer(
                CreateImFixedArrayFromArray(
                    context.Namespace,
                    argument.ArrayBounds,
                    argumentTypeArray.ElementType
                ).AsEntityReference(),
                false
            );

        return argument;

        static Struct CreateImFixedArrayFromArray(string @namespace, string arrayBounds, IEntityReference arrayElementType) {
            return new Struct {
                Kind = Struct.StructKind.Struct,
                Name = $"ImFixedArray{arrayBounds}",
                Namespace = @namespace,
                GenericArguments = [arrayElementType]
            };
        }
    }

    public static void DisableVarArgsFunctionsAndDelegates<T>(T functionLike) where T : IFunctionLike {
        IReadOnlyList<string> bannedTypeNames = ["varargs", "va_list"];

        bool hasBannedArgumentType = functionLike.Arguments.Any(
            arg => {
                string typeName = FormatType(arg.Type.Value, new FormatTypeOptions { DelegatesAsFunctionPointers = true });
                return bannedTypeNames.Any(banned => typeName.Contains(banned));
            }
        );

        if (!hasBannedArgumentType)
            return;

        functionLike.IsDisabled = true;
        functionLike.Comments =
            functionLike.Comments
                .WithAddedNotes("DISABLED: varargs is not supported by .NET");
    }

    public static void RemoveBrokenConditionalDefines<T>(T functionLike) where T : IFunctionLike {
        IReadOnlyList<string> bannedConditionalExpression = [
            "defined(IMGUI_HAS_IMSTR)"
        ];

        functionLike.Conditionals =
            functionLike.Conditionals
                .Where(cond => !bannedConditionalExpression.Any(banned => cond.Expression.Contains(banned)))
                .ToArray();
    }

    public static void WrapFunctionsInConditional(
        IReadOnlyList<string> originalFullyQualifiedNames,
        Conditional conditional,
        Function function
    ) {
        if (!originalFullyQualifiedNames.Contains(function.OriginalFullyQualifiedName))
            return;

        function.Conditionals = [
            ..function.Conditionals,
            conditional
        ];
    }

    private static bool PatchArgumentDefaultValue(BindingsModel model, IFunctionLike functionLike, FunctionArgument argument) {
        Debug.Assert(argument.DeclarationDefaultValue != null);
        string initialDefaultValue = argument.DeclarationDefaultValue;

        argument.DefaultValue = initialDefaultValue;
        argument.DeclarationDefaultValue = initialDefaultValue;

        argument.DeclarationDefaultValue = argument.DeclarationDefaultValue switch {
            "NULL" => "null",
            "ImVec2(0, 0)" => "default",
            "ImVec4(0, 0, 0, 0)" => "default",
            "IM_COL32(255, 0, 0, 255)" => "IM_COL32_RED",
            _ => argument.DeclarationDefaultValue
        };

        if (argument.DeclarationDefaultValue != initialDefaultValue)
            return true;

        foreach (EntityReferenceWrapper<Enum> enumerationReference in model.Enums) {
            Enum enumeration = enumerationReference.Value;
            argument.DeclarationDefaultValue = argument.DeclarationDefaultValue.Replace($"{enumeration.Name}_", $"{enumeration.Name}.");
        }

        if (argument.DeclarationDefaultValue != initialDefaultValue)
            return true;

        Enum? argumentTypeEnum = (Enum?) argument.Type.ExpandWrappers<Enum>()?.Value;
        if (argumentTypeEnum != null) {
            // Find an enum member with matching string representation
            Enum.Member? matchingEnumMember =
                argumentTypeEnum.Members
                    .FirstOrDefault(member => member.Value == argument.DeclarationDefaultValue);

            if (matchingEnumMember != null) {
                argument.DeclarationDefaultValue = $"{argumentTypeEnum.Name}.{matchingEnumMember.Name}";

                //argument.DeclarationDefaultValue = $"({argumentTypeEnum.Name}) {argument.DeclarationDefaultValue}";
            } else {
                Log.LogWarning(
                    $"Could not find matching enum member '{argument.DeclarationDefaultValue}'" +
                    $" in enum '{argumentTypeEnum}'"
                );
            }
        }

        if (argument.DeclarationDefaultValue != initialDefaultValue)
            return true;

        ExpressionSyntax expressionSyntax = ParseExpression(argument.DeclarationDefaultValue);
        switch (expressionSyntax) {
            case LiteralExpressionSyntax literalExpressionSyntax:
                return literalExpressionSyntax.Kind() switch {
                    SyntaxKind.StringLiteralExpression => false, // FIXME: we'll need this later
                    _ => true
                };
            case PrefixUnaryExpressionSyntax:
            case IdentifierNameSyntax:
            case SizeOfExpressionSyntax:
                return true;
            case InvocationExpressionSyntax:
                return false;
            default:
                Log.LogWarning(
                    $"Argument '{argument.Name}' of function '{FormatType(functionLike)}' " +
                    $"has unexpected array bounds value '{argument.DefaultValue}' " +
                    $"that got understood as {expressionSyntax.GetType().Name}"
                );
                return false;
        }
    }
}

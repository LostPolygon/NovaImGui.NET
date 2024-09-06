using System.Diagnostics;
using CaseConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CSharpCodeGenerationUtility;
using static SyntaxFactoryCached;

public static class FunctionModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(FunctionModifiers));

    public static void PatchArgumentDefaultValues<T>(BindingsModel model, T functionLike) where T : IFunctionLike {
        int? lastArgumentWithInvalidDefaultValueIndex = null;
        for (int index = 0; index < functionLike.Arguments.Count; index++) {
            FunctionArgument argument = functionLike.Arguments[index];
            if (argument.DefaultValue == null)
                continue;

            PatchArgumentDefaultValue(model, functionLike, argument, out bool isDefaultValueValidInDeclaration);
            if (!isDefaultValueValidInDeclaration) {
                argument.IsDefaultValueValidInDeclaration = false;
                lastArgumentWithInvalidDefaultValueIndex = index;
            }
        }

        if (lastArgumentWithInvalidDefaultValueIndex == null)
            return;

        for (int index = 0; index <= lastArgumentWithInvalidDefaultValueIndex; index++) {
            FunctionArgument argument = functionLike.Arguments[index];
            argument.IsDefaultValueValidInDeclaration = false;
        }
    }

    public static string FixInvalidArgumentNames(string argumentName, int argumentIndex) {
        argumentName =
            String.IsNullOrEmpty(argumentName) ?
                $"arg{argumentIndex + 1}" :
                FixInvalidIdentifier(argumentName);

        return argumentName;
    }

    public static FunctionArgument PrettifyArgumentName(FunctionArgument argument) {
        if (!argument.Name.StartsWith("__")) {
            argument.Name = argument.Name.SnakeCaseToCamelCase();
        }

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
        if (argument.Type.Value is not Array argumentTypeArray || String.IsNullOrEmpty(argumentTypeArray.ArrayBounds))
            return argument;

        if (argumentTypeArray.ArrayBoundsNumeric == null)
            throw new InvalidOperationException($"Argument '{argument}' doesn't have numeric array bounds set");

        bool canBeConvertedToImVec =
            argumentTypeArray.ElementType.Value is Builtin { BuiltinName: "float" } &&
            argumentTypeArray.ArrayBoundsNumeric is >= 2 and <= 4;
        if (canBeConvertedToImVec) {
            string imVecName = $"ImVec{argumentTypeArray.ArrayBoundsNumeric}";
            argument.Type.Value =
                new Pointer(
                    new Struct {
                        Kind = Struct.StructKind.Struct,
                        Name = imVecName,
                        Namespace = context.RootNamespace,
                        ByValue = true,
                        NoEmit = true
                    }.AsEntityReference(),
                    false
                );
        } else {
            argument.Type.Value =
                new Pointer(
                    CreateImFixedArrayFromArray(
                        context.RootNamespace,
                        argumentTypeArray.ArrayBoundsNumeric.Value,
                        argumentTypeArray.ElementType
                    ).AsEntityReference(),
                    false
                );
        }

        return argument;

        static Struct CreateImFixedArrayFromArray(string @namespace, long arrayBounds, IEntityReference arrayElementType) {
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

    private static void PatchArgumentDefaultValue(
        BindingsModel model,
        IFunctionLike functionLike,
        FunctionArgument argument,
        out bool isDefaultValueValidInDeclaration
    ) {
        Debug.Assert(argument.DefaultValue != null);

        isDefaultValueValidInDeclaration = true;
        string initialDefaultValue = argument.DefaultValue;

        // FIXME: make it an argument
        argument.DefaultValue = initialDefaultValue switch {
            "NULL" => "default",
            "ImVec2(0, 0)" => "default",
            "ImVec4(0, 0, 0, 0)" => "default",
            "IM_COL32(255, 0, 0, 255)" => "IM_COL32_RED",
            _ => argument.DefaultValue
        };

        if (argument.DefaultValue != initialDefaultValue)
            return;

        foreach (EntityReferenceWrapper<Enum> enumerationReference in model.Enums) {
            Enum enumeration = enumerationReference.Value;
            argument.DefaultValue = argument.DefaultValue.Replace($"{enumeration.Name}_", $"{enumeration.Name}.");
        }

        if (argument.DefaultValue != initialDefaultValue)
            return;

        Enum? argumentTypeEnum = (Enum?) argument.Type.ExpandAllTypedefsAndPointers<Enum>()?.Value;
        if (argumentTypeEnum != null) {
            // Find an enum member with matching string representation
            Enum.Member? matchingEnumMember =
                argumentTypeEnum.Members
                    .FirstOrDefault(member => member.Value == argument.DefaultValue);

            if (matchingEnumMember != null) {
                argument.DefaultValue = $"{argumentTypeEnum.Name}.{matchingEnumMember.Name}";
            } else {
                Log.LogWarning(
                    $"Could not find matching enum member '{argument.DefaultValue}'" +
                    $" in enum '{argumentTypeEnum}'"
                );
            }
        }

        if (argument.DefaultValue != initialDefaultValue)
            return;

        // FIXME: make it a list of options
        switch (argument.DefaultValue) {
            case "ImVec2(1, 0)":
            case "ImVec2(1, 1)":
            case "ImVec2(0, 1)":
            case "ImVec2(0.0f, 0.0f)":
            case "ImVec4(1, 1, 1, 1)":
            case "ImVec2(-FLT_MIN, 0)":
                argument.DefaultValue = "new " + argument.DefaultValue;
                isDefaultValueValidInDeclaration = false;
                return;
        }

        ExpressionSyntax expressionSyntax = ParseExpression(argument.DefaultValue);
        switch (expressionSyntax) {
            case LiteralExpressionSyntax literalExpressionSyntax:
                switch (literalExpressionSyntax.Kind()) {
                    case SyntaxKind.StringLiteralExpression:
                        isDefaultValueValidInDeclaration = false;
                        return; // FIXME: we'll need this later
                    default:
                        return;
                }
            case PrefixUnaryExpressionSyntax:
            case IdentifierNameSyntax:
            case SizeOfExpressionSyntax:
                return;
            case InvocationExpressionSyntax:
                isDefaultValueValidInDeclaration = false;
                return;
            default:
                Log.LogWarning(
                    $"Argument '{argument.Name}' of function '{FormatType(functionLike)}' " +
                    $"has unexpected array bounds value '{argument.DefaultValue}' " +
                    $"that got understood as {expressionSyntax.GetType().Name}"
                );
                isDefaultValueValidInDeclaration = false;
                return;
        }
    }
}

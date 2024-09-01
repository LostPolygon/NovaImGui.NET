using CaseConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CSharpCodeGenerationUtility;
using static SyntaxFactory;

public static class FunctionModifiers {
    public static FunctionArgument PatchArgumentDefaultValues(BindingsModel model, IFunctionLike functionLike, FunctionArgument argument) {
        string initialDefaultValue = argument.DefaultValue;
        if (initialDefaultValue == null)
            return argument;

        argument.DefaultValueForFuture = initialDefaultValue;
        argument.DefaultValue = initialDefaultValue;

        argument.DefaultValue = argument.DefaultValue switch {
            "NULL" => "null",
            "ImVec2(0, 0)" => "default",
            _ => argument.DefaultValue
        };

        if (argument.DefaultValue != initialDefaultValue)
            return argument;

        foreach (IEntityReference<Enum> enumerationReference in model.Enums) {
            Enum enumeration = enumerationReference.Value;
            argument.DefaultValue = argument.DefaultValue.Replace($"{enumeration.Name}_", $"{enumeration.Name}.");
        }

        if (argument.DefaultValue != initialDefaultValue)
            return argument;

        if (argument.Type.ToString().Contains("ImGuiPopupFlags")) {
            Console.Write(5);
        }

        // FIXME: this doesn't work because some enums like ImGuiPopupFlags are typedefd
        if (argument.Type.Value is Enum argumentTypeEnum) {
            argument.DefaultValue = $"({argumentTypeEnum.Name}) {argument.DefaultValue}";
        }

        if (argument.DefaultValue != initialDefaultValue)
            return argument;

        ExpressionSyntax expressionSyntax = ParseExpression(argument.DefaultValue);

        //argument.DefaultValue = null;

        return argument;
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

    // TODO: merge with one below
    public static FunctionArgument PatchArrayArgumentsToUseByReference(FunctionArgument argument) {
        if (argument.Type is not IEntityReference<Array> arrayType)
            return argument;

        //if (argument.ArrayBounds == null)
        //    return argument;

        if (arrayType.Value.ArrayBounds == null) {
            // 1. Convert boundless arrays to pointer
            argument = argument with {
                Type = new Pointer(arrayType.Value.ElementType, false).AsEntityReference()
            };
        } else {
            // 2. in C, passing fixed size array to a function is actually still passing a pointer.
            // So let's make it actually pass a pointer.
            argument = argument with {
                Type = new Pointer(arrayType.Value.ElementType, false).AsEntityReference()
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
        if (argument.Type is not IEntityReference<Array> argumentTypeArray || String.IsNullOrEmpty(argument.ArrayBounds))
            return argument;

        return argument with {
            Type = new Pointer(
                CreateImFixedArrayFromArray(context, argument.ArrayBounds, argumentTypeArray.Value.ElementType.Value).AsEntityReference(),
                false
            ).AsEntityReference()
        };
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
        IReadOnlyList<string> bannedConditionalExpression = ["defined(IMGUI_HAS_IMSTR)"];

        functionLike.Conditionals =
            functionLike.Conditionals
                .Where(cond => !bannedConditionalExpression.Any(banned => cond.Expression.Contains(banned)))
                .ToEqualityList();
    }
}

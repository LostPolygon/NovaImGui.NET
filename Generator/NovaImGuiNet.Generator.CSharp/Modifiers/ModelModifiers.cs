using CaseConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static SyntaxFactory;
using static CSharpCodeGenerationUtility;
using static MathUtility;

public static class ModelModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(StructModifiers));

    public static BindingsModel GenerateDelegatesForInlineFunctionArguments(BindingsContext context, BindingsModel model) {
        IReadOnlyList<Function> allFunctions = model.GetAllFunctions().ToArray().Select(x => x.Value).ToArray();

        foreach (Function function in allFunctions) {
            foreach (FunctionArgument argument in function.Arguments) {
                if (argument.Type is not { Value: Delegate argumentTypeDelegate })
                    continue;

                string newDelegateName =
                    "ImGui" +
                    function.Name.RemoveFromStart("ImGui_") +
                    argument.Name.SnakeCaseToCamelCase().ToPascalCase();

                Delegate newDelegate = argumentTypeDelegate with {
                    Name = newDelegateName
                };

                Typedef newDelegateTypedef = new(new Comments(), [], false) {
                    Name = newDelegateName,
                    Namespace = context.Namespace, // FIXME: need a better way to determine namespace
                    Type = newDelegate.AsEntityReference()
                };
                IEntityReference<Typedef> newDelegateTypedefReference = newDelegateTypedef.AsEntityReference();

                argument.Type = newDelegateTypedefReference;
                model.Typedefs.Add(newDelegateTypedefReference);
            }
        }

        return model;
    }
}

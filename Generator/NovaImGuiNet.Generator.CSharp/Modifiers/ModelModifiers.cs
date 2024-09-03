using CaseConverter;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;

public static class ModelModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(ModelModifiers));

    public static BindingsModel AssignNamespaces(
        BindingsContext context,
        BindingsModel model,
        IReadOnlyDictionary<string, string> fileNameToNamespace
    ) {
        IEnumerable<ITopLevelEntity> topLevelEntities =
            model
            .Children
            .Select(x => x.Value)
            .OfType<ITopLevelEntity>();

        foreach (ITopLevelEntity entity in topLevelEntities) {
            if (entity.SkipEmit)
                continue;

            if (entity.SourceLocation == null)
                throw new InvalidOperationException($"Entity '{entity} has no source location information'");

            if (!fileNameToNamespace.TryGetValue(entity.SourceLocation.Filename, out string? targetNamespace))
                throw new InvalidOperationException($"File name '{entity.SourceLocation.Filename}' has no target namespace");

            entity.Namespace = targetNamespace;
        }

        return model;
    }

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

                Typedef newDelegateTypedef = new(new Comments(), [], null, false) {
                    Name = newDelegateName,
                    Namespace = function.Namespace,
                    Type = newDelegate.AsEntityReference()
                };
                IEntityReference newDelegateTypedefReference = newDelegateTypedef.AsEntityReference();

                argument.Type = newDelegateTypedefReference;
                model.Children.Add(newDelegateTypedefReference);
            }
        }

        return model;
    }
}

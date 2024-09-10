using CaseConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static SyntaxFactory;
using static CSharpCodeGenerationUtility;
using static SyntaxFactoryCached;

public static class ModelModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(ModelModifiers));

    public static BindingsModel AssignNamespaces(BindingsContext context, BindingsModel model) {
        IEnumerable<ITopLevelEntity> topLevelEntities =
            model
                .Children
                .Select(x => x.Value)
                .OfType<ITopLevelEntity>();

        foreach (ITopLevelEntity entity in topLevelEntities) {
            if (entity.NoEmit)
                continue;

            if (entity.SourceLocation == null)
                throw new InvalidOperationException($"Entity '{entity} has no source location information'");

            string targetNamespace = context.Customizations.RequireCustomization(entity.SourceLocation).Namespace;
            entity.Namespace = targetNamespace;
        }

        return model;
    }

    public static BindingsModel MakePointersToStructsOpaque(BindingsContext context, BindingsModel model) {
        IGrouping<string, Function>[] functionsBySourceLocations =
            model.GetAllFunctions()
                .ToArray()
                .Select(x => x.Value)
                .GroupBy(x => x.SourceLocation.Filename)
                .ToArray();

        foreach (IGrouping<string, Function> functionsBySourceLocation in functionsBySourceLocations) {
            BindingsFileCustomizations customizations =
                context.Customizations.RequireCustomization(functionsBySourceLocation.Key);

            foreach (Function function in functionsBySourceLocation) {
                if (function.Name.Contains("InitForSDLRenderer")) {
                    Console.Write(6);
                }

                foreach (FunctionArgument argument in function.Arguments) {
                    if (argument.Type.Value is not Pointer pointer)
                        continue;

                    string? originalType = null;

                    // Second level pointer
                    bool updated = UpdateType(customizations, pointer.ValueType, out IEntityReference? newPointerValueType);
                    if (updated) {
                        originalType = FormatType(pointer);
                        pointer.ValueType.Value = newPointerValueType.Value;
                    } else {
                        updated = UpdateType(customizations, argument.Type, out IEntityReference? newArgumentType);
                        if (updated) {
                            originalType = FormatType(argument.Type.Value);
                            argument.Type = newArgumentType;
                        }
                    }

                    if (updated) {
                        argument.Attributes.Add(
                            Attribute(
                                ParseName("ImGuiOriginalType"),
                                AttributeArgumentList(
                                    SeparatedList(
                                        [
                                            AttributeArgument(
                                                LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    Literal(originalType)
                                                )
                                            )
                                        ]
                                    )
                                )
                            )
                        );
                    }
                }
            }
        }

        return model;

        static bool UpdateType(BindingsFileCustomizations customizations, IEntityReference type, out IEntityReference? newType) {
            newType = null;
            if (type.Value is not Pointer pointer)
                return false;

            // FIXME: why does stuff like SDL_Event become a Builtin instead of Struct?
            string? pointerValueTypeName =
                (pointer.ValueType.Value as Struct)?.Name ??
                (pointer.ValueType.Value as Builtin)?.BuiltinName;

            if (pointerValueTypeName == null ||
                !customizations.MakePointersToStructsOpaque.Contains(pointerValueTypeName))
                return false;

            newType = KnownEntities.IntPtr.AsEntityReference();
            return true;
        }
    }

    public static BindingsModel GenerateDelegatesForInlineFunctionArguments(BindingsModel model) {
        IReadOnlyList<Function> allFunctions = model.GetAllFunctions().ToArray().Select(x => x.Value).ToArray();

        foreach (Function function in allFunctions) {
            foreach (FunctionArgument argument in function.Arguments) {
                if (argument.Type is not { Value: Delegate argumentTypeDelegate })
                    continue;

                string newDelegateName =
                    "ImGui" +
                    function.Name.RemovePrefix("ImGui_") +
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

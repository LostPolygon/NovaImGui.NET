using System.Diagnostics.CodeAnalysis;
using CaseConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator.CSharp.Templates;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static SyntaxFactory;
using static CSharpCodeGenerationUtility;
using static SyntaxFactoryCached;

public static class ModelModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(ModelModifiers));

    public static void GenerateHandleTypes(BindingsContext context, BindingsModel model) {
        // Generate handles
        foreach (BindingsFileCustomizations fileCustomizations in context.Customizations.BindingsFileCustomizations) {
            foreach (BindingsFileCustomizations.TreatTypeAsHandle handle in fileCustomizations.GenerateHandleTypes) {
                CompilationUnitWithFileInformation generatedHandle =
                    handle.ValueType == null ?
                        GeneratePointerHandle(fileCustomizations.Namespace, handle.Name) :
                        GenerateValueHandle(fileCustomizations.Namespace, handle.Name, handle.ValueType);

                model.ExtraCompilationUnits.Add(generatedHandle);
            }
        }

        // Check if there were any matching typedefs and if so, rewrite
        foreach (EntityReferenceWrapper<Typedef> typedef in model.Typedefs) {
            if (typedef.Value.SourceLocation == null)
                continue;

            BindingsFileCustomizations fileCustomizations =
                context.Customizations
                    .RequireCustomization(typedef.Value.SourceLocation);
            BindingsFileCustomizations.TreatTypeAsHandle? treatTypeAsHandle =
                fileCustomizations
                    .GenerateHandleTypes
                    .FirstOrDefault(x => x.Name == typedef.Value.Name);

            if (treatTypeAsHandle == null)
                continue;

            if (treatTypeAsHandle.ValueType == null) {
                typedef.Value.Type = KnownEntities.IntPtr.AsEntityReference();
            } else {
                typedef.Value.Type =
                    new Builtin(treatTypeAsHandle.ValueType, null, StorageClass.NotSpecified)
                        .AsEntityReference();
            }

            typedef.Value.NoEmit = true;
        }
    }

    public static void AssignNamespaces(BindingsContext context, BindingsModel model) {
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
    }

    public static void MakePointersToStructsOpaque(BindingsContext context, BindingsModel model) {
        IGrouping<string, Function>[] functionsBySourceLocations =
            model.GetAllFunctions()
                .ToArray()
                .Select(x => x.Value)
                .GroupBy(x => x.SourceLocation!.Filename)
                .ToArray();

        foreach (IGrouping<string, Function> functionsBySourceLocation in functionsBySourceLocations) {
            BindingsFileCustomizations customizations =
                context.Customizations.RequireCustomization(functionsBySourceLocation.Key);

            foreach (Function function in functionsBySourceLocation) {
                foreach (FunctionArgument argument in function.Arguments) {
                    if (argument.Type.Value is not Pointer pointer)
                        continue;

                    string? originalType = null;

                    // Second level pointer
                    bool updated = UpdateType(customizations, pointer.ValueType, out IEntityReference? newPointerValueType);
                    if (updated) {
                        originalType = FormatType(pointer);
                        pointer.ValueType.Value = newPointerValueType!.Value;
                    } else {
                        updated = UpdateType(customizations, argument.Type, out IEntityReference? newArgumentType);
                        if (updated) {
                            originalType = FormatType(argument.Type.Value);
                            argument.Type = newArgumentType!;
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
                                                    Literal(originalType!)
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

        static bool UpdateType(
            BindingsFileCustomizations customizations,
            IEntityReference type,
            [NotNullWhen(true)] out IEntityReference? newType
        ) {
            newType = null;
            if (type.Value is not Pointer pointer)
                return false;

            IEntityReference pointerValueType = pointer.ValueType.ExpandTypedef()!;

            // FIXME: why does stuff like SDL_Event become a Builtin instead of Struct?
            string? pointerValueTypeName =
                (pointerValueType.Value as Struct)?.Name ??
                (pointerValueType.Value as Builtin)?.BuiltinName;

            if (pointerValueTypeName == null ||
                !TextMatchUtility.IsMatchAny(pointerValueTypeName, customizations.MakePointersToStructsOpaque))
                return false;

            newType = KnownEntities.IntPtr.AsEntityReference();
            return true;
        }
    }

    public static void GenerateDelegatesForInlineFunctionArguments(BindingsModel model) {
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
    }

    private static CompilationUnitWithFileInformation GeneratePointerHandle(
        string targetNamespace,
        string typeName
    ) {
        PointerHandleTemplate template = new() {
            Session = new Dictionary<string, object> {
                [nameof(PointerHandleTemplate.TypeNamespace)] = targetNamespace,
                [nameof(PointerHandleTemplate.TypeName)] = typeName,
            }
        };

        template.Initialize();
        string transformText = template.TransformText();

        CompilationUnitSyntax compilationUnitSyntax = ParseCompilationUnit(transformText);
        return
            new CompilationUnitWithFileInformation(
                compilationUnitSyntax,
                new CompilationUnitFileInformation(
                    typeName,
                    targetNamespace,
                    "Structs"
                )
            );
    }

    private static CompilationUnitWithFileInformation GenerateValueHandle(
        string targetNamespace,
        string typeName,
        string valueType
    ) {
        ValueHandleTemplate template = new() {
            Session = new Dictionary<string, object> {
                [nameof(ValueHandleTemplate.TypeNamespace)] = targetNamespace,
                [nameof(ValueHandleTemplate.TypeName)] = typeName,
                [nameof(ValueHandleTemplate.ValueType)] = valueType,
            }
        };

        template.Initialize();
        string transformText = template.TransformText();

        CompilationUnitSyntax compilationUnitSyntax = ParseCompilationUnit(transformText);
        return
            new CompilationUnitWithFileInformation(
                compilationUnitSyntax,
                new CompilationUnitFileInformation(
                    typeName,
                    targetNamespace,
                    "Structs"
                )
            );
    }
}

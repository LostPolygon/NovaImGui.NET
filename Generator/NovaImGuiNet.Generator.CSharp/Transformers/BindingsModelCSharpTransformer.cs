using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CommonModifiers;
using static StructModifiers;
using static FunctionModifiers;
using static TypedefModifiers;
using static EnumModifiers;

public class BindingsModelCSharpTransformer {
    private static readonly ILogger Log = Logging.Create<BindingsModelCSharpTransformer>();

    public BindingsModelCSharpTransformer(
        BindingsModel model,
        MetadataDto metadataDto,
        BindingsContext context
    ) {
        MetadataDto = metadataDto;
        Context = context;
        Model = model;
    }

    private BindingsModel Model { get; set; }
    private MetadataDto MetadataDto { get; }
    private BindingsContext Context { get; }

    public BindingsModel Transform() {
        Dictionary<string, string> fileNameToNamespace = new() {
            ["imgui.h"] = "NovaImGuiNet",
            ["imgui_internal.h"] = "NovaImGuiNet.Internal",
            ["imstb_textedit_minimal.h"] = "NovaImGuiNet.Internal",
            ["imgui_impl_sdl2.h"] = "NovaImGuiNet.Backend.Sdl2",
            ["imgui_impl_sdlrenderer2.h"] = "NovaImGuiNet.Backend.Sdl2Renderer",
            ["imgui_impl_win32.h"] = "NovaImGuiNet.Backend.Win32",
        };

        using (Operation("Assigning namespaces")) {
            Model = ModelModifiers.AssignNamespaces(Context, Model, fileNameToNamespace);
        }

        using (Operation("Genericizing structs")) {
            Model = GenericizeStructsByFieldType(Model, "ImVector", "Data", true);
            Model = GenericizeStructsByFieldType(Model, "ImSpan", "Data", true);
            Model = GenericizeStructByTypeName(Model, "ImChunkStream_ImGuiTableSettings", "ImChunkStream", "ImGuiTableSettings");
            Model = GenericizeStructByTypeName(Model, "ImChunkStream_ImGuiWindowSettings", "ImChunkStream", "ImGuiWindowSettings");
            Model = GenericizeStructByTypeName(Model, "ImPool_ImGuiMultiSelectState", "ImPool", "ImGuiMultiSelectState");
            Model = GenericizeStructByTypeName(Model, "ImPool_ImGuiTabBar", "ImPool", "ImGuiTabBar");
            Model = GenericizeStructByTypeName(Model, "ImPool_ImGuiTable", "ImPool", "ImGuiTable");
        }

        using (Operation("Renaming anonymous types")) {
            Model = RenameStructFieldType(Model, "ImGuiInputEvent", "__anonymous_type", "ImGuiInputEventData", "EventData");
            Model = RenameStructFieldType(Model, "ImGuiStoragePair", "__anonymous_type", "ImGuiStoragePairValue", "Value");
            Model = RenameStructFieldType(Model, "ImGuiStyleMod", "__anonymous_type", "ImGuiStyleModBackupValue", "BackupValue");
        }

        Transform("Patching broken typedefs", Model.Typedefs, PatchBrokenTypedefs);

        using (Operation("Rewriting typedefs to enums")) {
            Model = UpdateEnumTypedefs(Model);
        }

        // Enums
        Transform("Adding public members to internal enums", Model.Enums, x => AddPublicMembersToPrivateEnum(Model, x));
        Transform("Cleaning up enum identifiers", Model.Enums, CleanupIdentifiers);
        Transform("Patching enum values referencing other enums", Model.Enums, PatchEnumsReferencingOtherEnums);
        Transform("Wrapping enums in shared conditionals", Model.Enums, x => WrapInSharedConditional(x, x.Members));

        // Struct fields
        Transform(
            "Wrapping structs in shared conditionals",
            Model.Structs,
            x => WrapInSharedConditional(x, x.Children.OfType<IIndependentEntity>().ToArray())
        );

        using (Operation("Patching enum reference in fixed array fields")) {
            Model.Structs.ForEach(structure =>
                structure.Value.Children
                    .UpdateInPlace<IEntity, Field>(field =>
                        PatchEnumReferencesInFieldArraySizeDeclaration(Model, field)
                    )
            );
        }

        Transform(
            "Manually patching tricky numeric array bounds in fixed array fields",
            Model.Structs,
            structure => PatchManualMissingFieldArrayNumericBounds(Model, structure)
        );
        Transform(
            "Calculating numeric array bounds in fixed array fields",
            Model.Structs,
            structure => FixMissingFieldArrayNumericBounds(Context, MetadataDto, Model, structure)
        );
        Transform(
            "Exploding fixed size array fields with non-primitive types into multiple fields",
            Model.Structs,
            ExplodeFixedSizeArrayFieldsOfNonPrimitiveTypes
        );

        if (Context.Options.CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes) {
            Transform(
                "Creating Span accessor properties for fixed size array fields with primitive types",
                Model.Structs,
                CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes
            );
        }

        Transform(
            "Convert bitfields into property accessors",
            Model.Structs,
            ConvertBitFields
        );
        Transform(
            "Adding IntPtr accessors to fields of function pointer types",
            Model.Structs,
            AddUnmanagedFunctionPointerFieldRawPtrAccessor
        );

        using (Operation("Prettifying delegate names")) {
            Model.Typedefs
                .Where(x => x.Reference.ExpandTypedefsAndPointers<Delegate>() != null)
                .ForEach(bindingsTypedef => {
                    EntityReferenceWrapper<Delegate> bindingsDelegateReference =
                        new(bindingsTypedef.Reference.ExpandTypedefsAndPointers<Delegate>()!);
                    bindingsDelegateReference.Reference.Value = PrettifyDelegateName(bindingsDelegateReference.Value);
                    bindingsTypedef.Value.Name = bindingsDelegateReference.Value.Name;
                });
        }

        using (Operation("Generating delegates for inline function definitions in function arguments")) {
            Model = ModelModifiers.GenerateDelegatesForInlineFunctionArguments(Context, Model);
        }

        Transform(
            "Workaround .NET recursive generic struct bug",
            Model.Structs,
            x => WorkaroundDotNetRecursiveGenericStructBug(Model, x)
        );

        // Functions and typedef delegates
        IReadOnlyList<IFunctionLike> allFunctionsAnDelegates =
            new List<IEntityReference>([
                    ..Model.GetAllFunctions().Select(x => x.Reference),
                    ..Model.Typedefs
                        .Where(typedef => typedef.Value.Type.Value is Delegate)
                        .Select(typedef => typedef.Value.Type)
                ])
                .Select(x => x.Value)
                .Cast<IFunctionLike>()
                .ToArray();

        void UpdateFunctionArguments(Func<IFunctionLike, FunctionArgument, int, FunctionArgument> transformer) {
            allFunctionsAnDelegates.ForEach(function => {
                function.Arguments.UpdateInPlace<FunctionArgument, FunctionArgument>(
                    (arg, argIndex) => transformer(function, arg, argIndex)
                );
            });
        }

        using (Operation("Patching function default values")) {
            allFunctionsAnDelegates.ForEach(x => PatchArgumentDefaultValues(Model, x));
        }

        using (Operation("Fixing invalid argument names")) {
            UpdateFunctionArguments((_, arg, argIndex) => {
                arg.Name = FixInvalidArgumentNames(arg.Name, argIndex);
                return arg;
            });
        }

        using (Operation("Prettifying argument names")) {
            UpdateFunctionArguments((function, arg, _) =>
                PrettifyArgumentName(function, arg)
            );
        }

        using (Operation("Converting C-style fixed-size array arguments to pointers to fixed-size struct")) {
            UpdateFunctionArguments((_, arg, _) =>
                ConvertFixedSizeArrayArgumentToFixedSizeStruct(Context, arg)
            );
        }

        using (Operation("Convert C-style unbound array arguments to pointers")) {
            UpdateFunctionArguments((_, arg, _) =>
                PatchArrayArgumentsToUseByReference(arg)
            );
        }

        using (Operation("Remove variadic functions and delegates")) {
            allFunctionsAnDelegates.ForEach(DisableVarArgsFunctionsAndDelegates);
        }

        using (Operation("Remove broken conditional defines around some functions")) {
            allFunctionsAnDelegates.ForEach(RemoveBrokenConditionalDefines);
        }

        using (Operation("FIXME: Disabling broken functions")) {
            // ImGuiListClipperRange can't be used as a return value,
            // since it contains a bool field and that's broken when
            // runtime marshaling is not disabled
            // https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib1050-1069
            allFunctionsAnDelegates
                .OfType<Function>()
                .ForEach(function =>
                    WrapFunctionsInConditional(
                        [
                            "ImGuiListClipperRange::FromPositions",
                            "ImGuiListClipperRange::FromIndices"
                        ],
                        new Conditional("false"),
                        function
                    )
                );
        }

        using (Operation("Generating ImGuiConstants class")) {
            Model.Children.AddRange(
                new ImGuiConstantsGenerator(Context, MetadataDto)
                    .Generate(fileNameToNamespace)
                    .Select(x => x.AsEntityReference())
            );
        }

        using (Operation("Generating managed methods")) {
            Model = ManagedMethodGenerator.Generate(Model);
        }

        using (Operation("Disable ImVec")) {
            Model.Structs
                .Where(x => x.Value.Name is "ImVec2" or "ImVec4")
                .ForEach(x => x.Value.NoEmit = true);
        }

        return Model;
    }

    private static TimedLoggedOperation Operation(string operationName) {
        return new TimedLoggedOperation(Log, operationName);
    }

    private static void Transform<TEntity>(
        string operationName,
        IReadOnlyList<EntityReferenceWrapper<TEntity>> entityReferences,
        Func<TEntity, TEntity?> transformer
    ) where TEntity : Entity {
        using TimedLoggedOperation timedLoggedOperation = new(Log, operationName);
        BindingsModelTransformUtility.Transform(
            entityReferences.Select(x => x.Reference).ToList(),
            transformer
        );
    }
}

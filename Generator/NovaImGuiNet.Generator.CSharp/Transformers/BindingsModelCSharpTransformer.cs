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
        using (new TimedLoggedOperation(Log, "Genericizing structs")) {
            Model = GenericizeStructsByFieldType(Model, "ImVector", "Data", true);
            Model = GenericizeStructsByFieldType(Model, "ImSpan", "Data", true);
            Model = GenericizeStructByTypeName(Model, "ImChunkStream_ImGuiTableSettings", "ImChunkStream", "ImGuiTableSettings");
            Model = GenericizeStructByTypeName(Model, "ImChunkStream_ImGuiWindowSettings", "ImChunkStream", "ImGuiWindowSettings");
            Model = GenericizeStructByTypeName(Model, "ImPool_ImGuiMultiSelectState", "ImPool", "ImGuiMultiSelectState");
            Model = GenericizeStructByTypeName(Model, "ImPool_ImGuiTabBar", "ImPool", "ImGuiTabBar");
            Model = GenericizeStructByTypeName(Model, "ImPool_ImGuiTable", "ImPool", "ImGuiTable");
        }

        using (new TimedLoggedOperation(Log, "Renaming anonymous types")) {
            Model = RenameStructFieldType(Model, "ImGuiInputEvent", "__anonymous_type", "ImGuiInputEventData", "EventData");
            Model = RenameStructFieldType(Model, "ImGuiStoragePair", "__anonymous_type", "ImGuiStoragePairValue", "Value");
            Model = RenameStructFieldType(Model, "ImGuiStyleMod", "__anonymous_type", "ImGuiStyleModBackupValue", "BackupValue");
        }

        Transform("Patching broken typedefs", Model.Typedefs, PatchBrokenTypedefs);

        using (new TimedLoggedOperation(Log, "Rewriting typedefs to enums")) {
            Model = RewriteTypedefsToEnums(Model);
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

        using (new TimedLoggedOperation(Log, "Patching enum reference in fixed array fields")) {
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

        using (new TimedLoggedOperation(Log, "Prettifying delegate names")) {
            Model.Typedefs
                .Where(x => x.Reference.ExpandWrappers<Delegate>() != null)
                .ForEach(bindingsTypedef => {
                    EntityReferenceWrapper<Delegate> bindingsDelegateReference =
                        new(bindingsTypedef.Reference.ExpandWrappers<Delegate>()!);
                    bindingsDelegateReference.Reference.Value = PrettifyDelegateName(bindingsDelegateReference.Value);
                    bindingsTypedef.Value.Name = bindingsDelegateReference.Value.Name;
                });
        }

        using (new TimedLoggedOperation(Log, "Generating delegates for inline function definitions in function arguments")) {
            Model = ModelModifiers.GenerateDelegatesForInlineFunctionArguments(Context, Model);
        }

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

        using (new TimedLoggedOperation(Log, "Patching function default values")) {
            allFunctionsAnDelegates.ForEach(x => PatchArgumentDefaultValues(Model, x));
        }

        using (new TimedLoggedOperation(Log, "Fixing invalid argument names")) {
            UpdateFunctionArguments((_, arg, argIndex) => {
                arg.Name = FixInvalidArgumentNames(arg.Name, argIndex);
                return arg;
            });
        }

        using (new TimedLoggedOperation(Log, "Prettifying argument names")) {
            UpdateFunctionArguments((function, arg, _) =>
                PrettifyArgumentName(function, arg)
            );
        }

        using (new TimedLoggedOperation(Log, "Converting C-style fixed-size array arguments to pointers to fixed-size struct")) {
            UpdateFunctionArguments((_, arg, _) =>
                ConvertFixedSizeArrayArgumentToFixedSizeStruct(Context, arg)
            );
        }

        using (new TimedLoggedOperation(Log, "Convert C-style unbound array arguments to pointers")) {
            UpdateFunctionArguments((_, arg, _) =>
                PatchArrayArgumentsToUseByReference(arg)
            );
        }

        using (new TimedLoggedOperation(Log, "Remove variadic functions and delegates")) {
            allFunctionsAnDelegates.ForEach(DisableVarArgsFunctionsAndDelegates);
        }

        using (new TimedLoggedOperation(Log, "Remove broken conditional defines around some functions")) {
            allFunctionsAnDelegates.ForEach(RemoveBrokenConditionalDefines);
        }

        using (new TimedLoggedOperation(Log, "Generating ImGuiConstants class")) {
            Struct imGuiConstantsStruct = new ImGuiConstantsGenerator(Context, MetadataDto).Generate();
            Model.Children.Add(imGuiConstantsStruct.AsEntityReference());
        }

        // TODO: fix ImGuiWindow self-referencing through ImGuiWindowTempData.ChildWindows:
        // https://github.com/dotnet/runtime/issues/6924

        return Model;
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

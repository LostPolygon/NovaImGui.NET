using System.Runtime.InteropServices.ComTypes;
using Microsoft.Extensions.Logging;
using MoreLinq.Extensions;
using NovaImGuiNet.Generator.CSharp.ManagedMethods;
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
        using (Operation("Assigning namespaces")) {
            ModelModifiers.AssignNamespaces(Context, Model);
        }

        using (Operation("Removing prefixes from type names")) {
            TopLevelEntity[] types = [
                ..Model.Structs.Select(x => x.Value),
                ..Model.Enums.Select(x => x.Value)
            ];

            foreach (TopLevelEntity type in types) {
                BindingsFileCustomizations bindingsFileCustomizations =
                    Context.Customizations.RequireCustomizationByNamespace(type.Namespace!);

                foreach (string removedPrefix in bindingsFileCustomizations.PrefixesRemovedFromTypeNames) {
                    type.Name = type.Name.RemovePrefix(removedPrefix);
                }
            }
        }

        using (Operation("Genericizing structs")) {
            GenericizeStructsByFieldType(Model, "ImVector", "Data", true);
            GenericizeStructsByFieldType(Model, "ImSpan", "Data", true);
            GenericizeStructByTypeName(Model, "ImChunkStream_ImGuiTableSettings", "ImChunkStream", "ImGuiTableSettings");
            GenericizeStructByTypeName(Model, "ImChunkStream_ImGuiWindowSettings", "ImChunkStream", "ImGuiWindowSettings");
            GenericizeStructByTypeName(Model, "ImPool_ImGuiMultiSelectState", "ImPool", "ImGuiMultiSelectState");
            GenericizeStructByTypeName(Model, "ImPool_ImGuiTabBar", "ImPool", "ImGuiTabBar");
            GenericizeStructByTypeName(Model, "ImPool_ImGuiTable", "ImPool", "ImGuiTable");
        }

        using (Operation("Renaming anonymous types")) {
            RenameStructFieldType(Model, "ImGuiInputEvent", "__anonymous_type", "ImGuiInputEventData", "EventData");
            RenameStructFieldType(Model, "ImGuiStoragePair", "__anonymous_type", "ImGuiStoragePairValue", "Value");
            RenameStructFieldType(Model, "ImGuiStyleMod", "__anonymous_type", "ImGuiStyleModBackupValue", "BackupValue");
        }

        using (Operation("Generating handle types")) {
            ModelModifiers.GenerateHandleTypes(Context, Model);
        }

        Transform(
            "Rewriting typedefs to enums",
            Model.Typedefs,
            x => UpdateEnumTypedefs(x, Model)
        );

        // Enums
        Transform("Adding public members to internal enums", Model.Enums, x => AddPublicMembersToPrivateEnum(x, Model));
        Transform("Cleaning up enum identifiers", Model.Enums, CleanupEnumIdentifiers);
        Transform("Patching enum values referencing other enums", Model.Enums, PatchEnumsReferencingOtherEnums);
        Transform(
            "Wrapping enums in shared conditionals",
            Model.Enums,
            x => WrapInSharedConditional(x, x.Members)
        );

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
                        PatchEnumReferencesInFieldArraySizeDeclaration(field, Model)
                    )
            );
        }

        Transform(
            "Manually patching tricky numeric array bounds in fixed array fields",
            Model.Structs,
            structure => PatchManualMissingFieldArrayNumericBounds(structure, Model)
        );

        Transform(
            "Calculating numeric array bounds in fixed array fields",
            Model.Structs,
            structure => FixMissingFieldArrayNumericBounds(structure, MetadataDto, Context, Model)
        );

        Transform(
            "Exploding fixed size array fields with non-primitive types into multiple fields",
            Model.Structs,
            ExplodeFixedSizeArrayFieldsOfNonPrimitiveTypes
        );

        Transform(
            "Converting bitfields into property accessors",
            Model.Structs,
            ConvertBitFields
        );

        Transform(
            "Adding NullTerminatedString properties to string-like fields",
            Model.Structs,
            AddNullTerminatedStringPropertyForConstCharPointerFields
        );

        Transform(
            "Converting void* fields to IntPtr",
            Model.Structs,
            ConvertVoidPointerFieldsToIntPtr
        );

        Transform(
            "Adding pointer wrapper properties to fields of non-void pointer types",
            Model.Structs,
            structure => AddPointerWrapperAccessorPropertyForPointerFields(structure, Context)
        );

        if (Context.Options.CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes) {
            Transform(
                "Creating Span accessor properties for fixed size array fields with primitive types",
                Model.Structs,
                CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes
            );
        }

        using (Operation("Generating delegates for inline function definitions in function arguments")) {
            ModelModifiers.GenerateDelegatesForInlineFunctionArguments(Model);
        }

        using (Operation("Prettifying delegate and delegate argument names")) {
            Model.Typedefs
                .Where(x => x.Reference.ExpandAllTypedefsAndPointers<Delegate>() != null)
                .ForEach(delegateType => {
                    EntityReferenceWrapper<Delegate> delegateReference =
                        new(delegateType.Reference.ExpandAllTypedefsAndPointers<Delegate>()!);
                    delegateReference.Reference.Value = PrettifyDelegateName(delegateReference.Value);

                    delegateReference.Value.Arguments =
                        delegateReference.Value.Arguments
                            .Select(PrettifyArgumentName)
                            .ToList();

                    delegateType.Value.Name = delegateReference.Value.Name;
                });
        }

        Transform(
            "Workaround .NET recursive generic struct bug",
            Model.Structs,
            x => WorkaroundDotNetRecursiveGenericStructBug(x, Model)
        );

        // Functions and typedef delegates
        IReadOnlyList<IFunctionLike> allFunctionsAndDelegates =
            new List<IEntityReference>([
                    ..Model.GetAllFunctions().Select(x => x.Reference),
                    ..Model.GetAllDelegates().Select(x => x.Reference),
                ])
                .Select(x => x.Value)
                .Cast<IFunctionLike>()
                .ToArray();

        void UpdateFunctionsAndDelegatesArguments(Func<IFunctionLike, FunctionArgument, int, FunctionArgument> transformer) {
            allFunctionsAndDelegates.ForEach(function => {
                function.Arguments.UpdateInPlace<FunctionArgument, FunctionArgument>(
                    (arg, argIndex) => transformer(function, arg, argIndex)
                );
            });
        }

        using (Operation("Patching function argument default values")) {
            allFunctionsAndDelegates.ForEach(x => PatchArgumentDefaultValues(x, Model));
        }

        using (Operation("Fixing invalid function arguments names")) {
            UpdateFunctionsAndDelegatesArguments((_, arg, argIndex) => {
                arg.Name = FixInvalidArgumentNames(arg.Name, argIndex);
                return arg;
            });
        }

        using (Operation("Fixing unblittable delegate arguments")) {
            UpdateFunctionsAndDelegatesArguments((functionLike, arg, _) => {
                if (functionLike is not Delegate)
                    return arg;

                // bool is not blittable. Normally we would just do [MarshalAs(UnmanagedType.I1)],
                // but we cant do that in a function pointer, so instead we wrap the byte in a ByteBool wrapper.
                // FIXME: do we need to do the same to the return value?
                if (arg.Type.Value is Builtin builtin && builtin.ManagedType == typeof(bool)) {
                    arg.Type = KnownEntities.ByteBool.AsEntityReference();
                }

                return arg;
            });
        }

        using (Operation("Converting void* native function arguments to IntPtr")) {
            UpdateFunctionsAndDelegatesArguments((_, arg, _) =>
                ConvertVoidPointerArgumentsToIntPtr(arg)
            );
        }

        using (Operation("Converting void* native function return values to IntPtr")) {
            allFunctionsAndDelegates.ForEach(ConvertVoidPointerReturnValuesToIntPtr);
        }

        using (Operation("Converting function pointer native function return values to IntPtr")) {
            allFunctionsAndDelegates.ForEach(ConvertFunctionPointerReturnValuesToIntPtr);
        }

        using (Operation("Converting C-style fixed-size array arguments to pointers to fixed-size structs")) {
            UpdateFunctionsAndDelegatesArguments((_, arg, _) =>
                ConvertFixedSizeArrayArgumentToFixedSizeStruct(arg, Context)
            );
        }

        using (Operation("Converting C-style unbound array arguments to pointers")) {
            UpdateFunctionsAndDelegatesArguments((_, arg, _) =>
                PatchArrayArgumentsToUseByReference(arg)
            );
        }

        using (Operation("Remove variadic functions and delegates")) {
            allFunctionsAndDelegates.ForEach(DisableVarArgsFunctionsAndDelegates);
        }

        Transform(
            "Adding IntPtr accessors to fields of function pointer types",
            Model.Structs,
            AddIntPtrAccessorPropertyForUnmanagedFunctionPointerFields
        );

        using (Operation("Removing broken conditional defines around some functions")) {
            allFunctionsAndDelegates.ForEach(RemoveBrokenConditionalDefines);
        }

        using (Operation("FIXME: Disabling broken functions")) {
            // ImGuiListClipperRange can't be used as a return value,
            // since it contains a bool field and that's broken when
            // runtime marshalling is not disabled
            // https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib1050-1069
            /*allFunctionsAnDelegates
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
                );*/
        }

        using (Operation("Making pointers to chosen structs opaque")) {
            ModelModifiers.MakePointersToStructsOpaque(Context, Model);
        }

        using (Operation("Generating ImGuiConstants class")) {
            Model.Children.AddRange(
                new ImGuiConstantsGenerator(Context, MetadataDto)
                    .Generate()
                    .Select(x => x.AsEntityReference())
            );
        }

        using (Operation("Generating managed methods")) {
            Model = new ManagedMethodGenerator(Context, Model).Generate();
        }

        using (Operation("Disabling NoEmit structs")) {
            Model.Structs
                .Where(structure => structure.Value.SourceLocation != null)
                .Where(structure => {
                    BindingsFileCustomizations customizations =
                        Context.Customizations
                            .RequireCustomization(structure.Value.SourceLocation);

                    return TextMatchUtility.IsMatchAny(structure.Value.Name, customizations.NoEmitStructs);
                })
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

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

public partial class BindingsModelFromMetadataResolver(
    MetadataModel metadataModel,
    BindingsContext bindingsContext
) {
    private static readonly ILogger Log = Logging.Create<BindingsModelFromMetadataResolver>();

    private Dictionary<MetadataModel.IEntityReference, BindingsModel.IEntityReference> MetadataEntityToBindingsEntity { get; } = new();

    public event Action<MetadataModel.Entity>? UnresolvedTypeDetected;

    public BindingsModel Resolve() {
        ResolveAllModelEntities();
        BindingsModel bindingsModel = new(MetadataEntityToBindingsEntity.Values.ToList());
        return bindingsModel;
    }

    private void ResolveAllModelEntities() {
        foreach (var entityReference in metadataModel.Enums) {
            ResolveEntity(entityReference);
        }

        foreach (var entityReference in metadataModel.Typedefs) {
            ResolveEntity(entityReference);
        }

        foreach (var entityReference in metadataModel.Structs) {
            ResolveEntity(entityReference);

            foreach (var function in entityReference.Value.Functions) {
                ResolveEntity(function);
            }
        }

        foreach (var entityReference in metadataModel.FreeFunctions) {
            ResolveEntity(entityReference);
        }
    }

    private BindingsModel.Entity ConvertEntity(MetadataModel.IEntityReference metadataRaw) {
        return metadataRaw switch {
            MetadataModel.IEntityReference<MetadataModel.Enum> metadata => ResolveEnum(metadata.Value),
            MetadataModel.IEntityReference<MetadataModel.Array> metadata => ResolveArray(metadata),
            MetadataModel.IEntityReference<MetadataModel.Builtin> metadata => ResolveBuiltinType(metadata.Value),
            MetadataModel.IEntityReference<MetadataModel.Pointer> metadata => ResolvePointer(metadata),
            MetadataModel.IEntityReference<MetadataModel.Delegate> metadata => ResolveDelegate(metadata),
            MetadataModel.IEntityReference<MetadataModel.Function> metadata => ResolveFunction(metadata),
            MetadataModel.IEntityReference<MetadataModel.Struct> metadata => ResolveStruct(metadata),
            MetadataModel.IEntityReference<MetadataModel.Typedef> metadata => ResolveTypedef(metadata),
            _ => throw new ArgumentOutOfRangeException(nameof(metadataRaw))
        };
    }

    private BindingsModel.IEntityReference ResolveEntity(MetadataModel.IEntityReference metadata) {
        ArgumentNullException.ThrowIfNull(metadata);

        if (MetadataEntityToBindingsEntity.TryGetValue(metadata, out BindingsModel.IEntityReference? bindingsEntityRaw))
            return bindingsEntityRaw;

        BindingsModel.IEntityReference bindingsReference = BindingsModel.CreateEmptyEntityReference();
        bool added = MetadataEntityToBindingsEntity.TryAdd(metadata, bindingsReference);
        Debug.Assert(added);

        // Convert the DTO to model entity and assign it to the reference
        BindingsModel.Entity userEntity = ConvertEntity(metadata);
        bindingsReference.Value = userEntity;

        return bindingsReference;
    }
}

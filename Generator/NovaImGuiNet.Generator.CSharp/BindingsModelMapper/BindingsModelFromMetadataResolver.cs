using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

public partial class BindingsModelFromMetadataResolver(
    MetadataModel metadataModel
) {
    private static readonly ILogger Log = Logging.Create<BindingsModelFromMetadataResolver>();

    private Dictionary<MetadataModel.IEntityReference, BindingsModel.IEntityReference> MetadataEntityToBindingsEntity { get; } = new();

    public event Action<MetadataModel.Entity>? UnresolvedTypeDetected;

    public BindingsModel Resolve() {
        ResolveAllModelEntities();

        List<BindingsModel.IEntityReference> topLevelEntities =
            MetadataEntityToBindingsEntity.Values
                .ToList()
                .Where(x => x.Value is BindingsModel.Enum or BindingsModel.Typedef or BindingsModel.Struct or BindingsModel.Function)
                .ToList();

        BindingsModel bindingsModel = new(topLevelEntities);
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

    private BindingsModel.Entity ConvertEntity(MetadataModel.IEntityReference metadata) =>
        metadata.Value switch {
            MetadataModel.Enum value => ResolveEnum(value),
            MetadataModel.Array value => ResolveArray(value),
            MetadataModel.Builtin value => ResolveBuiltinType(value),
            MetadataModel.Pointer value => ResolvePointer(value),
            MetadataModel.Delegate value => ResolveDelegate(value),
            MetadataModel.Function value => ResolveFunction(value),
            MetadataModel.Struct value => ResolveStruct(value),
            MetadataModel.Typedef value => ResolveTypedef(value),
            _ => throw new ArgumentOutOfRangeException(nameof(metadata), metadata, null)
        };

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

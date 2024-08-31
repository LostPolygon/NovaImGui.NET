namespace NovaImGuiNet.Generator.Metadata;

public static class MetadataModelExtensions {
    public static MetadataModel.IEntityReference AsEntityReference(this MetadataModel.Entity entity) =>
        ((MetadataModel.IConvertibleToEntityReference) entity).AsEntityReference();
}

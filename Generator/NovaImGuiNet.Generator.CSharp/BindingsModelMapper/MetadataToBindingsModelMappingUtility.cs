using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

public static class MetadataToBindingsModelMappingUtility {
    public static IReadOnlyList<BindingsModel.Conditional> Map(IEnumerable<MetadataModel.Conditional> metadata) =>
        metadata.Select(Map).ToArray();

    public static BindingsModel.Conditional Map(MetadataModel.Conditional metadata) =>
        new(metadata.Condition, metadata.Expression);

    public static BindingsModel.Comments Map(MetadataModel.Comments metadata) =>
        new(metadata.Preceding, metadata.Attached, []);

    public static BindingsModel.SourceLocation? Map(MetadataModel.SourceLocation? metadata) {
        if (metadata != null) {
            return new(metadata.Value.Filename, metadata.Value.Line);
        } else {
            return null;
        }
    }
}

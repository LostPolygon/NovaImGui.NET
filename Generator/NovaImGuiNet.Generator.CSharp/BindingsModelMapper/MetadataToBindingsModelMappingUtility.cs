using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

public static class MetadataToBindingsModelMappingUtility {
    public static EqualityList<BindingsModel.Conditional> Map(IEnumerable<MetadataModel.Conditional> metadata) =>
        metadata.Select(Map).ToEqualityList();

    public static BindingsModel.Conditional Map(MetadataModel.Conditional metadata) =>
        new(
            metadata.Condition,
            metadata.Expression
        );

    public static BindingsModel.Comments Map(MetadataModel.Comments metadata) =>
        new(
            metadata.Preceding,
            metadata.Attached,
            metadata.Notes
        );
}

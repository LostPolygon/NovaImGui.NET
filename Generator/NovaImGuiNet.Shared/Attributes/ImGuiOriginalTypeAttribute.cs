namespace NovaImGuiNet;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field)]
public class ImGuiOriginalTypeAttribute(string originalTypeName) : Attribute {
    public string OriginalTypeName { get; } = originalTypeName;
}

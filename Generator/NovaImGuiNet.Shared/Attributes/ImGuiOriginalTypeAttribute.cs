namespace NovaImGuiNet;

[AttributeUsage(AttributeTargets.Parameter)]
public class ImGuiOriginalTypeAttribute(string originalTypeName) : Attribute {
    public string OriginalTypeName { get; } = originalTypeName;
}

namespace NovaImGuiNet;

public class ImGuiOriginalTypeAttribute(string originalTypeName) : Attribute {
    public string OriginalTypeName { get; } = originalTypeName;
}

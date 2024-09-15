namespace NovaImGuiNet;

public class ImGuiDefaultValueAttribute(string description) : Attribute {
    public string Description { get; } = description;
}

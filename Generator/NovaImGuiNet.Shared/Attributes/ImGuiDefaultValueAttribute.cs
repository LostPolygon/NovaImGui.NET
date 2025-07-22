namespace NovaImGuiNet;

[AttributeUsage(AttributeTargets.Parameter)]
public class ImGuiDefaultValueAttribute(string description) : Attribute {
    public string Description { get; } = description;
}

namespace NovaImGuiNet.Generator.CSharp;

public record CompilationUnitFileInformation(
    string Name,
    string Namespace,
    string? Category = null,
    bool IsPreformatted = false,
    bool ForceTopLevel = false
);

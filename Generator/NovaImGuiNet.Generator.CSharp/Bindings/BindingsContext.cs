namespace NovaImGuiNet.Generator.CSharp;

public record BindingsContext(
    string RootNamespace,
    string RootOutputPath,
    BindingsGenerationOptions Options
);

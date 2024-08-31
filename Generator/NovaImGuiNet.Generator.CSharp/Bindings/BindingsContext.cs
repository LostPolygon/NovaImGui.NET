namespace NovaImGuiNet.Generator.CSharp;

public record BindingsContext(
    string Namespace,
    string InternalNamespace,
    string RootOutputPath,
    BindingsGenerationOptions Options
);

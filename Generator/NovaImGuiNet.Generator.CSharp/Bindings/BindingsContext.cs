namespace NovaImGuiNet.Generator.CSharp;

public record BindingsContext(
    string RootOutputPath,
    string RootNamespace,
    BindingsCustomizationsContext Customizations,
    BindingsGenerationOptions Options
) {
    public ValueSubstitutions<long> NumericValueSubstitutions { get; } = new(value => value.ToString());
}

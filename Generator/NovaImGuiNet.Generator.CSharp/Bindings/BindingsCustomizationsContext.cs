namespace NovaImGuiNet.Generator.CSharp;

public record BindingsCustomizationsContext(
    IReadOnlyList<BindingsFileCustomizations> BindingsFileCustomizations
) {
    public IReadOnlyDictionary<string, BindingsFileCustomizations> OriginalFileNameToCustomizations { get; } =
        BindingsFileCustomizations.ToDictionary(x => x.OriginalHeaderFileName, x => x);

    public BindingsFileCustomizations? GetCustomization(string headerFileName) {
        ArgumentException.ThrowIfNullOrWhiteSpace(headerFileName, nameof(headerFileName));
        return OriginalFileNameToCustomizations.GetValueOrDefault(headerFileName);
    }

    public BindingsFileCustomizations RequireCustomization(string headerFileName) =>
        GetCustomization(headerFileName) ??
        throw new InvalidOperationException($"No customizations found for header file '{headerFileName}'");

    public BindingsFileCustomizations? GetCustomization(BindingsModel.SourceLocation? sourceLocation) =>
        GetCustomization(sourceLocation?.Filename!);

    public BindingsFileCustomizations RequireCustomization(BindingsModel.SourceLocation? sourceLocation) =>
        RequireCustomization(sourceLocation?.Filename!);

    public BindingsFileCustomizations RequireCustomizationByNamespace(string ns) =>
        BindingsFileCustomizations.FirstOrDefault(x => x.Namespace == ns) ??
        throw new InvalidOperationException($"No customizations found for namespace'{ns}'");
}

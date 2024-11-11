namespace NovaImGuiNet.Generator.CSharp;

public record BindingsGenerationOptions {
    public CodeFormatMode CodeFormatMode { get; init; }
    public bool CreateSpanAccessorForFixedSizeArrayFieldsOfPrimitiveTypes { get; init; }
    public bool GenerateXmlDocumentationComments { get; init; }
    public bool GenerateManagedStringFunctionArguments { get; init; } = true;
}

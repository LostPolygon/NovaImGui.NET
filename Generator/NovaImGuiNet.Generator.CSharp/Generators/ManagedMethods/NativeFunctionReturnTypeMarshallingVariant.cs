namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static BindingsModel;

public readonly record struct NativeFunctionReturnTypeMarshallingVariant(
    Entity NativeFunctionReturnType,
    ReturnTypeMarshallingMode ReturnTypeMarshallingMode,
    string? ManagedMethodNameSuffix
) {
    public override string ToString() {
        List<string> elements = [];

        if (ReturnTypeMarshallingMode != ReturnTypeMarshallingMode.Passthrough) {
            elements.Add($"ReturnType = {ReturnTypeMarshallingMode}");
        }

        string suffix = ManagedMethodNameSuffix != null ? $"(suffix: {ManagedMethodNameSuffix}) " : "";
        return $"{suffix}{CSharpCodeGenerationUtility.FormatType(NativeFunctionReturnType)} ({String.Join(", ", elements)})";
    }
}

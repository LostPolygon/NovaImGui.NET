namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static BindingsModel;

public readonly record struct NativeFunctionReturnTypeMarshallingVariant(
    Entity NativeFunctionReturnType,
    ReturnTypeMarshallingMode ReturnTypeMarshallingMode
) {
    public override string ToString() {
        List<string> elements = [];

        if (ReturnTypeMarshallingMode != ReturnTypeMarshallingMode.Passthrough) {
            elements.Add($"ReturnType = {ReturnTypeMarshallingMode}");
        }

        return $"{CSharpCodeGenerationUtility.FormatType(NativeFunctionReturnType)} ({String.Join(", ", elements)})";
    }
}

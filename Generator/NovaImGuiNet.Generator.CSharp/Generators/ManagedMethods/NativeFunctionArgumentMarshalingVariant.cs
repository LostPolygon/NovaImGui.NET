namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public readonly record struct NativeFunctionArgumentMarshalingVariant(
    BindingsModel.FunctionArgument NativeFunctionArgument,
    int NativeFunctionArgumentIndex,
    StringMarshalingMode StringMarshalingMode,
    PointerMarshalMode PointerMarshalMode
) {
    public override string ToString() {
        List<string> elements = [];

        if (StringMarshalingMode != StringMarshalingMode.Undefined) {
            elements.Add($"String = {StringMarshalingMode}");
        }

        if (PointerMarshalMode != PointerMarshalMode.Undefined) {
            elements.Add($"Pointer = {PointerMarshalMode}");
        }

        return $"{NativeFunctionArgument.Name} ({String.Join(", ", elements)})";
    }
}

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public readonly record struct NativeFunctionArgumentMarshallingVariant(
    BindingsModel.FunctionArgument NativeFunctionArgument,
    int NativeFunctionArgumentIndex,
    StringMarshallingMode StringMarshallingMode,
    PointerMarshallingMode PointerMarshallingMode
) {
    public override string ToString() {
        List<string> elements = [];

        if (StringMarshallingMode != StringMarshallingMode.Undefined) {
            elements.Add($"String = {StringMarshallingMode}");
        }

        if (PointerMarshallingMode != PointerMarshallingMode.Undefined) {
            elements.Add($"Pointer = {PointerMarshallingMode}");
        }

        return $"{NativeFunctionArgument.Name} ({String.Join(", ", elements)})";
    }
}

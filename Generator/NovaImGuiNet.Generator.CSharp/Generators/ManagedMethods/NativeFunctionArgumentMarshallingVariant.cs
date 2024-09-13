namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public record NativeFunctionArgumentMarshallingVariant(
    BindingsModel.FunctionArgument NativeFunctionArgument,
    int NativeFunctionArgumentIndex,
    StringMarshallingMode StringMarshallingMode,
    PointerMarshallingMode PointerMarshallingMode,
    StringBeginEndPairMode StringBeginEndPairMode
) {
    public override string ToString() {
        List<string> elements = [];

        if (StringMarshallingMode != StringMarshallingMode.Undefined) {
            elements.Add($"String = {StringMarshallingMode}");
        }

        if (PointerMarshallingMode != PointerMarshallingMode.Undefined) {
            elements.Add($"Pointer = {PointerMarshallingMode}");
        }

        if (StringBeginEndPairMode != StringBeginEndPairMode.None) {
            elements.Add($"BeginEndPair = {StringBeginEndPairMode}");
        }

        return $"{NativeFunctionArgument.Name} ({String.Join(", ", elements)})";
    }
}

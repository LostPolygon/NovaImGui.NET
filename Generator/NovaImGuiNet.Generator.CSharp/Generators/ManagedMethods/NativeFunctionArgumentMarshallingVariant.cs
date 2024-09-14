namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public record NativeFunctionArgumentMarshallingVariant(
    BindingsModel.FunctionArgument NativeFunctionArgument,
    int NativeFunctionArgumentIndex,
    ArgumentMarshallingMode ArgumentMarshallingMode,
    StringBeginEndPairMode StringBeginEndPairMode
) {
    public override string ToString() {
        List<string> elements = [];

        if (ArgumentMarshallingMode != ArgumentMarshallingMode.Passthrough) {
            elements.Add($"Argument = {ArgumentMarshallingMode}");
        }

        if (StringBeginEndPairMode != StringBeginEndPairMode.None) {
            elements.Add($"BeginEndPair = {StringBeginEndPairMode}");
        }

        return $"{NativeFunctionArgument.Name} ({String.Join(", ", elements)})";
    }
}

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerOutArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    protected override string InvocationVariable => InternalVar("_out_native");

    public override ArgumentMarshallerOutput Marshall() =>
        new(
            $"(IntPtr) {InvocationVariable}",
            [
                // TODO: ImFontGlyphRangesBuilder.BuildRanges crashes if ranges are not zero-initialized,
                // but check if there's an automated way to skip the zero-initialization
                $"{Argument.MarshalledArgument.Name} = default;",

                ..StartFixedBlock(
                    $"{FormatTypeDefault(Argument.MarshalledArgument.Type.Value)}*",
                    InvocationVariable,
                    $"&{Argument.MarshalledArgument.Name}"
                ),
            ],
            [ArgumentMarshallerCommand.BlockEnd]
        );
}

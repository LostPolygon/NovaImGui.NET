namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerRefArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    protected override string InvocationVariable => InternalVar("_ptr");

    public override ArgumentMarshallerOutput Marshall() {
        return new ArgumentMarshallerOutput(
            $"(IntPtr) {InvocationVariable}",
            [
                ..StartFixedBlock(
                    $"{MarshalledArgumentType}*",
                    InvocationVariable,
                    $"&{Argument.MarshalledArgument.Name}"
                )
            ],
            [ArgumentMarshallerCommand.BlockEnd]
        );
    }
}

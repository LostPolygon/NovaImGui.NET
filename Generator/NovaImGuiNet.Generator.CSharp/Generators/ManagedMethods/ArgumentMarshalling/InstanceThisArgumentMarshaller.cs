namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class InstanceThisArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        return new ArgumentMarshallerOutput(
            $"(IntPtr) {InvocationVariable}",
            [
                ..StartFixedBlock(
                    $"{NativeStruct!.Name}*",
                    InvocationVariable,
                    "&this"
                )
            ],
            [ArgumentMarshallerCommand.BlockEnd]
        );
    }
}

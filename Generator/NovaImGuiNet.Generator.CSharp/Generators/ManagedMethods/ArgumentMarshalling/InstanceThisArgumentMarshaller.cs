namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class InstanceThisArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        return new ArgumentMarshallerOutput(
            InvocationVariable,
            [$"{NativeStruct!.Name}* {InvocationVariable} = ({NativeStruct!.Name}*) Unsafe.AsPointer(ref this);"]
        );
    }
}

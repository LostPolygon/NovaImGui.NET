namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class InstanceThisArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() {
        return [$"{NativeStruct!.Name}* {InvocationVariableName} = ({NativeStruct!.Name}*) Unsafe.AsPointer(ref this);"];
    }
}

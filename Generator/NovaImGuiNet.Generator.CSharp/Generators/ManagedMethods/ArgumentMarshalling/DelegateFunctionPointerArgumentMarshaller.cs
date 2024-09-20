using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class DelegateFunctionPointerArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        Debug.Assert(Argument.IsPartOfDeclaration || Argument.MarshalledArgument.DefaultValue == null);
        return new ArgumentMarshallerOutput(
            $"(IntPtr) {Argument.MarshalledArgument.Name}",
            []
        );
    }
}

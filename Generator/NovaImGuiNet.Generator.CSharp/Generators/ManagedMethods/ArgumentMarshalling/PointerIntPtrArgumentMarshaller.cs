using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerIntPtrArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        Debug.Assert(Argument.IsPartOfDeclaration);
        return new ArgumentMarshallerOutput(
            $"({NativeArgumentType}) {Argument.MarshalledArgument.Name}",
            []
        );
    }
}

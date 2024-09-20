using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PassthroughArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        return new ArgumentMarshallerOutput(
            Argument.IsPartOfDeclaration ?
                Argument.MarshalledArgument.Name :
                RequireNonNull(Argument.MarshalledArgument.DefaultValue),
            []
        );
    }
}

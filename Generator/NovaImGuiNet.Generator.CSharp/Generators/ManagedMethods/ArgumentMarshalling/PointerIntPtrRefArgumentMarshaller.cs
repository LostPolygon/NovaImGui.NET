using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerIntPtrRefArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        Debug.Assert(Argument.IsPartOfDeclaration);
        return new ArgumentMarshallerOutput(
            InvocationVariable,
            [
                $"{NativeArgumentType} {InvocationVariable} = " +
                $"({NativeArgumentType}) Unsafe.AsPointer(ref {Argument.MarshalledArgument.Name});"
            ]
        );
    }
}

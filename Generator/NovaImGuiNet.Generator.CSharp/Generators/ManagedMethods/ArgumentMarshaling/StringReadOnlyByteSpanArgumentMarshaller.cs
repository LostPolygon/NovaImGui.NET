using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class StringReadOnlyByteSpanArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() {
        Debug.Assert(Argument.IsPartOfDeclaration);

        return [
            $"{NativeArgumentType} {InvocationVariableName} = " +
            $"InteropUtility.SpanToPointer({Argument.MarshaledArgument.Name});"
        ];
    }
}

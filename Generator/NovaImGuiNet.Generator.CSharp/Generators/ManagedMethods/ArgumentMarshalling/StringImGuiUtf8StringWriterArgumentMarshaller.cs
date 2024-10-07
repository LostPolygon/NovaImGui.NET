namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class StringImGuiUtf8StringWriterArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        if (!Argument.IsPartOfDeclaration)
            return new StringReadOnlyByteSpanArgumentMarshaller(MethodSignature, Argument).Marshall();

        bool needsNullTerminator = Argument.MarshallingVariant.StringBeginEndPairMode == StringBeginEndPairMode.None;
        return new ArgumentMarshallerOutput(
            InvocationVariable,
            [
                $"ref ImGuiUtf8StringWriter {InternalVar("_ref")} = ref ImGuiUtf8StringWriter.UnsafeAsRef({Argument.MarshalledArgument.Name});",

                $"ReadOnlySpan<byte> {InternalVar("_ref_span")} = " +
                $"{InternalVar("_ref")}.{(needsNullTerminator ? "GetFinalNullTerminatedWrittenSpan" : "GetFinalWrittenSpan")}();",

                $"{NativeArgumentType} {InvocationVariable} = UnsafeUtility.SpanToPointer({InternalVar("_ref_span")});"
            ],
            [$"{InternalVar("_ref")}.Free();"]
        );
    }
}

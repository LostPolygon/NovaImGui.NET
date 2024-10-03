namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class StringUtf8BytesStringWriterArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        if (!Argument.IsPartOfDeclaration)
            return new StringReadOnlyByteSpanArgumentMarshaller(MethodSignature, Argument).Marshall();

        return new ArgumentMarshallerOutput(
            InvocationVariable,
            [
                // FIXME: this is broken and doesn't really work. Zero terminator needs to be added after every string,
                // however this approach will only add it after the last string.
                $"{Argument.MarshalledArgument.Name}.AppendUtf8(\"\\0\"u8);", // append zero-terminator

                $"{Argument.MarshalledArgument.Name}.Flush();",
                $"ReadOnlySpan<byte> {InternalVar("_in_native_span")} = {Argument.MarshalledArgument.Name}.GetBufferWriter().WrittenSpan;",
                $"{NativeArgumentType} {InvocationVariable} = InteropUtility.SpanToPointer({InternalVar("_in_native_span")});"
            ]
        );
    }
}

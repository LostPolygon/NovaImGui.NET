using Microsoft.CodeAnalysis.CSharp;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class StringUtf8BytesStringWriterArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        if (!Argument.IsPartOfDeclaration)
            return new StringReadOnlyByteSpanArgumentMarshaller(MethodSignature, Argument).Marshall();

        return new ArgumentMarshallerOutput(
            InvocationVariable,
            [
                // Only append zero-terminator if the argument is not a part of begin-end pair
                //Argument.MarshallingVariant.StringBeginEndPairMode == StringBeginEndPairMode.None ?
                //    $"{Argument.MarshalledArgument.Name}.Utf8StringWriter.AppendUtf8(\"\\0\"u8);" :
                //    SyntaxFactory.Comment(""),

                //$"{Argument.MarshalledArgument.Name}.Flush();",
                //$"ReadOnlySpan<byte> {InternalVar("_in_native_span")} = {Argument.MarshalledArgument.Name}.GetBufferWriter().WrittenSpan;",
                $"ReadOnlySpan<byte> {InternalVar("_in_native_span")} = {Argument.MarshalledArgument.Name}.WrittenSpan;",
                $"{NativeArgumentType} {InvocationVariable} = InteropUtility.SpanToPointer({InternalVar("_in_native_span")});"
            ],
            [$"{Argument.MarshalledArgument.Name}.Free();"]
        );
    }
}

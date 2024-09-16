using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class StringReadOnlyCharSpanArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() {
        string value = Argument.MarshalledArgument.Name;
        if (!Argument.IsPartOfDeclaration) {
            Debug.Assert(Argument.MarshalledArgument.DefaultValue != null);
            value = $"(ReadOnlySpan<char>) {Argument.MarshalledArgument.DefaultValue}";
        } else if (
            Argument.MarshalledArgument.Type.Value is BindingsModel.Builtin builtin &&
            builtin.ManagedType == typeof(string)
        ) {
            value = $"{value}.AsSpan()";
        }

        string bufferBytesWrittenSizeVarDeclaration = "_";
        if (Argument.MarshallingVariant.StringBeginEndPairMode == StringBeginEndPairMode.BeginOnly) {
            bufferBytesWrittenSizeVarDeclaration = "int " + InternalVar("_native_utf8_buffer_bytes_written");
        }

        return [
            $"scoped Utf8StringMarshaller.ManagedToUnmanagedIn {InternalVar("_native_marshaller")} = new({value});",
            $"Span<byte> {InternalVar("_native_utf8_buffer")} = stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.StackAllocMaxSize];",
            $"{NativeArgumentType} {InvocationVariableName} = " +
            $"{InternalVar("_native_marshaller")}.ConvertToNative(" +
            $"{InternalVar("_native_utf8_buffer")}, out {bufferBytesWrittenSizeVarDeclaration});"
        ];
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateUnmarshalStatements() =>
        [$"{InternalVar("_native_marshaller")}.Free();"];
}

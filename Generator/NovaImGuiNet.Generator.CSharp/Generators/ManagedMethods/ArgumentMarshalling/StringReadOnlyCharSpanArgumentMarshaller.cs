//#define USE_V2


using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class StringReadOnlyCharSpanArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
#if USE_V2
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

        return new ArgumentMarshallerOutput(
            InvocationVariable,
            [
                $"Span<byte> {InternalVar("_native_utf8_buffer")} = stackalloc byte[Utf8StringMarshallerV2.ManagedToUnmanagedIn.StackAllocMaxSize];",
                $"{NativeArgumentType} {InvocationVariable} = " +
                $"Utf8StringMarshallerV2.ManagedToUnmanagedIn.ConvertToNative(" +
                $"{value}, {InternalVar("_native_utf8_buffer")}, out {bufferBytesWrittenSizeVarDeclaration}, out bool {InternalVar("_in_native_must_free")});"
            ],
            [$"if ({InternalVar("_in_native_must_free")}) {{\n Utf8StringMarshallerV2.ManagedToUnmanagedIn.Free({InvocationVariable}); \n}}"]
        );
#else
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

        return new ArgumentMarshallerOutput(
            InvocationVariable,
            [
                $"scoped Utf8StringMarshaller.ManagedToUnmanagedIn {InternalVar("_native_marshaller")} = new({value});",
                $"Span<byte> {InternalVar("_native_utf8_buffer")} = stackalloc byte[Utf8StringMarshaller.StackAllocMaxSize];",
                $"{NativeArgumentType} {InvocationVariable} = " +
                $"{InternalVar("_native_marshaller")}.ConvertToNative(" +
                $"{InternalVar("_native_utf8_buffer")}, out {bufferBytesWrittenSizeVarDeclaration});"
            ],
            [$"{InternalVar("_native_marshaller")}.Free();"]
        );
#endif
    }
}

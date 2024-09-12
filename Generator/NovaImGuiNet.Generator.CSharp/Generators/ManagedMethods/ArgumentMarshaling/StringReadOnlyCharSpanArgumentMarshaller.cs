using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class StringReadOnlyCharSpanArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() {
        string value = Argument.MarshaledArgument.Name;
        if (!Argument.IsPartOfDeclaration) {
            Debug.Assert(Argument.MarshaledArgument.DefaultValue != null);
            value = $"(ReadOnlySpan<char>) {Argument.MarshaledArgument.DefaultValue}";
        } else if (
            Argument.MarshaledArgument.Type.Value is BindingsModel.Builtin builtin &&
            builtin.ManagedType == typeof(string)
        ) {
            value = $"{value}.AsSpan()";
        }

        return [
            $"scoped Utf8StringMarshaller.ManagedToUnmanagedIn {InternalVar("_native_marshaller")} = new({value});",

            $"{NativeArgumentType} {InvocationVariableName} = " +
            $"{InternalVar("_native_marshaller")}.ConvertToNative(stackalloc byte[Utf8StringMarshaller.ManagedToUnmanagedIn.StackAllocMaxSize]);"
        ];
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateUnmarshalStatements() =>
        [$"{InternalVar("_native_marshaller")}.Free();"];
}

using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class StringReadOnlyByteSpanArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() {
        string value = Argument.MarshalledArgument.Name;
        if (!Argument.IsPartOfDeclaration) {
            Debug.Assert(Argument.MarshalledArgument.DefaultValue != null);

            string defaultValue = Argument.MarshalledArgument.DefaultValue;
            value =
                Argument.MarshalledArgument.DefaultValueKind == BindingsModel.FunctionArgumentDefaultValueKind.String ?
                    $"{defaultValue}u8" :
                    $"(ReadOnlySpan<byte>) {defaultValue}";
        }

        return [$"{NativeArgumentType} {InvocationVariableName} = InteropUtility.SpanToPointer({value});"];
    }
}

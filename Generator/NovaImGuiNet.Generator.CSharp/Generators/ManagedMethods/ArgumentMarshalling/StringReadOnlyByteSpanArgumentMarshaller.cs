using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class StringReadOnlyByteSpanArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        string value;
        if (Argument.IsPartOfDeclaration) {
            value = Argument.MarshalledArgument.Name;
        } else {
            Debug.Assert(Argument.MarshalledArgument.DefaultValue != null);

            string defaultValue = Argument.MarshalledArgument.DefaultValue;
            if (KnownEntities.NullishValues.Contains(defaultValue)) {
                return new ArgumentMarshallerOutput("default", []);
            }

            value = Argument.MarshalledArgument.DefaultValueKind == BindingsModel.FunctionArgumentDefaultValueKind.String ?
                $"{defaultValue}u8" :
                $"(ReadOnlySpan<byte>) {defaultValue}";
        }

        return new ArgumentMarshallerOutput(
            $"(IntPtr) {InvocationVariable}",
            [$"{NativeArgumentType} {InvocationVariable} = UnsafeUtility.SpanToPointer({value});"]
        );
    }
}

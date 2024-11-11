namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PassthroughReturnTypeMarshaller : ReturnTypeMarshaller {
    public PassthroughReturnTypeMarshaller(MethodSignature methodSignature, string nativeFunctionInvocation) : base(methodSignature, nativeFunctionInvocation) {
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateInvokeStatements() {
        if (CanSimplifyReturn())
            return [];

        return [
            $"{NativeReturnType} {ReturnVariableName};",
            $"{ReturnVariableName} = ({NativeReturnType}) {NativeFunctionInvocation};"
        ];
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateReturnStatements() {
        if (CanSimplifyReturn())
            return [$"return {NativeFunctionInvocation};"];

        if (NativeFunction.IsConstructor) {
            return [$"this = {ReturnVariableName};"];
        }

        return [$"return {ReturnVariableName};"];
    }

    private bool CanSimplifyReturn() {
        return
            !NativeFunction.IsConstructor &&
            (MethodSignature.Arguments.Count == 0 ||
                MethodSignature.Arguments.All(arg =>
                    arg.MarshallingVariant.ArgumentMarshallingMode is
                        ArgumentMarshallingMode.Passthrough or
                        ArgumentMarshallingMode.Ref or
                        ArgumentMarshallingMode.IntPtr or
                        ArgumentMarshallingMode.IntPtrRef or
                        ArgumentMarshallingMode.StringBytePointer or
                        ArgumentMarshallingMode.StringReadOnlyByteSpan or
                        ArgumentMarshallingMode.DelegateFunctionPointer or
                        ArgumentMarshallingMode.DelegateIntPtr
                )
            );
    }
}

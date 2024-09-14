namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PassthroughReturnTypeMarshaller : ReturnTypeMarshaller {
    public PassthroughReturnTypeMarshaller(MethodSignature methodSignature, string nativeFunctionInvocation) : base(methodSignature, nativeFunctionInvocation) {
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateInvokeStatements() {
        return [
            $"{NativeReturnType} {ReturnVariableName};",
            $"{ReturnVariableName} = {NativeFunctionInvocation};"
        ];
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateReturnStatements() {
        if (NativeFunction.IsConstructor) {
            return [$"this = {ReturnVariableName};"];
        }

        return [$"return {ReturnVariableName};"];
    }
}

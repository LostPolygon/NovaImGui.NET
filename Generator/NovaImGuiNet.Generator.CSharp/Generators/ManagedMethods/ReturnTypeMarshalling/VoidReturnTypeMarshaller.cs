namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class VoidReturnTypeMarshaller : ReturnTypeMarshaller {
    public VoidReturnTypeMarshaller(MethodSignature methodSignature, string nativeFunctionInvocation) : base(methodSignature, nativeFunctionInvocation) {
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateInvokeStatements() {
        return [$"{NativeFunctionInvocation};"];
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateReturnStatements() {
        return [];
    }
}

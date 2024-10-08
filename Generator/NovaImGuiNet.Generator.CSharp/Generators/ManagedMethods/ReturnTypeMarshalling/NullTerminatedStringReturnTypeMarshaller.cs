namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class NullTerminatedStringReturnTypeMarshaller : ReturnTypeMarshaller {
    public NullTerminatedStringReturnTypeMarshaller(MethodSignature methodSignature, string nativeFunctionInvocation) : base(methodSignature, nativeFunctionInvocation) {
    }

    protected override string CreateReturnVariableName() {
        return InternalVar("nts");
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateInvokeStatements() {
        return [
            $"{NativeReturnType} {InternalVar("value")};",
            $"{InternalVar("value")} = ({NativeReturnType}) {NativeFunctionInvocation};"
        ];
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateReturnStatements() {
        return [
            $"NullTerminatedString {ReturnVariableName} = new({InternalVar("value")});",
            $"return {ReturnVariableName};"
        ];
    }
}

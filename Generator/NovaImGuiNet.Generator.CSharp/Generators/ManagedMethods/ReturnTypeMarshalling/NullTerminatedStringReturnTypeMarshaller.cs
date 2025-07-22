namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class NullTerminatedUtf8StringReturnTypeMarshaller : ReturnTypeMarshaller {
    public NullTerminatedUtf8StringReturnTypeMarshaller(MethodSignature methodSignature, string nativeFunctionInvocation) : base(methodSignature, nativeFunctionInvocation) {
    }

    protected override string CreateReturnVariableName() => InternalVar("nts");

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateDeclarationStatements() => [
        $"{NativeReturnType} {InternalVar("value")};",
    ];

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateInvokeStatements() => [
        $"{InternalVar("value")} = ({NativeReturnType}) {NativeFunctionInvocation};"
    ];

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateReturnStatements() => [
        $"NullTerminatedUtf8String {ReturnVariableName} = new({InternalVar("value")});",
        $"return {ReturnVariableName};"
    ];
}

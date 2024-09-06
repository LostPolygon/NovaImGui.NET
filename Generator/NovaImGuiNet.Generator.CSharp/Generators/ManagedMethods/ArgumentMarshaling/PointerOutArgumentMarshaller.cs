namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerOutArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    protected override string CreateInvocationVariableName() {
        return InternalVar("_out_native_ptr");
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() => [
        $"{MarshaledArgumentType} {InternalVar("_out_native")};",
        $"{MarshaledArgumentType}* {InvocationVariableName} = &{InternalVar("_out_native")};",
    ];

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateUnmarshalStatements() =>
        [$"{Argument.MarshaledArgument.Name} = *{InvocationVariableName};"];
}

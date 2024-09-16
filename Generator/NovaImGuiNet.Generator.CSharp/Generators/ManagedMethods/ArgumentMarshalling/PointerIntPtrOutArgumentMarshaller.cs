namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerIntPtrOutArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() => [
        $"void* {InternalVar("_out_native")};",
        $"void** {InvocationVariableName} = &{InternalVar("_out_native")};"
    ];

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateUnmarshalStatements() =>
        [$"{Argument.MarshalledArgument.Name} = ({MarshalledArgumentType}) (*{InvocationVariableName});"];
}

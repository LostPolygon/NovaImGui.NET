namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public interface IReturnTypeMarshaller {
    string ReturnVariableName { get; }
    string NativeFunctionInvocation { get; }
    IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateDeclarationStatements();
    IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateInvokeStatements();
    IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateReturnStatements();
}

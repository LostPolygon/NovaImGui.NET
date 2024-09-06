namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public interface IArgumentMarshaller {
    string InvocationVariableName { get; }
    IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements();
    IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateUnmarshalStatements();
}

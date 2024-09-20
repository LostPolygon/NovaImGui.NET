namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public record ArgumentMarshallerOutput(
    string InvocationExpression,
    IReadOnlyList<ManagedMethodArgumentMarshallerStatement> MarshallStatements,
    IReadOnlyList<ManagedMethodArgumentMarshallerStatement>? UnmarshallStatements = null
) {
    public string InvocationExpression { get; } = InvocationExpression ?? throw new ArgumentNullException(nameof(InvocationExpression));
    public IReadOnlyList<ManagedMethodArgumentMarshallerStatement> MarshallStatements { get; } = MarshallStatements;
    public IReadOnlyList<ManagedMethodArgumentMarshallerStatement> UnmarshallStatements { get; } = UnmarshallStatements ?? [];
}

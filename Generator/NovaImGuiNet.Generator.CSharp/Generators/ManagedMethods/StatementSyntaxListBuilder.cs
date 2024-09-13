using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static SyntaxFactory;
using static SyntaxFactoryCached;

public class StatementSyntaxListBuilder {
    private readonly List<StatementSyntax> _statements = [];

    public IReadOnlyList<StatementSyntax> Statements => _statements;

    public void Append(ManagedMethodArgumentMarshallerStatement newStatement) {
        newStatement.Switch(
            statementText => _statements.Add(ParseStatementWithLeadingLineFeed(statementText)),
            statementSyntax => _statements.Add(statementSyntax),
            syntaxTrivia => {
                if (_statements.Count != 0) {
                    _statements[^1] = _statements[^1].AddTrailingTriviaToEnd(syntaxTrivia);
                }
            });
    }

    public void AppendRange(IReadOnlyList<ManagedMethodArgumentMarshallerStatement> newStatements) {
        foreach (ManagedMethodArgumentMarshallerStatement newStatement in newStatements) {
            Append(newStatement);
        }
    }
}

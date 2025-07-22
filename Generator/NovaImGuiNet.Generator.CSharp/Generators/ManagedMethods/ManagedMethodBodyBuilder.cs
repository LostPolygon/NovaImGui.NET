using OneOf;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static SyntaxFactory;
using static SyntaxFactoryCached;

public class ManagedMethodBodyBuilder {
    private readonly List<OneOf<StatementSyntax, ArgumentMarshallerCommand>> _statements = [];

    public void Append(ManagedMethodArgumentMarshallerStatement newStatement) {
        newStatement.Switch(
            statementText => _statements.Add(ParseStatementWithLeadingLineFeed(statementText)),
            statementSyntax => _statements.Add(statementSyntax),
            syntaxTrivia => {
                if (_statements.Count != 0) {
                    OneOf<StatementSyntax, ArgumentMarshallerCommand> lastStatement = _statements[^1];
                    if (lastStatement.IsT0) {
                        _statements[^1] = lastStatement.AsT0.AddTrailingTriviaToEnd(syntaxTrivia);
                    }
                }
            },
            command => _statements.Add(command));
    }

    public void AppendRange(IReadOnlyList<ManagedMethodArgumentMarshallerStatement> newStatements) {
        foreach (ManagedMethodArgumentMarshallerStatement newStatement in newStatements) {
            Append(newStatement);
        }
    }

    public IReadOnlyList<StatementSyntax> CreateStatements() => CreateStatements(_statements);

    private static IReadOnlyList<StatementSyntax> CreateStatements(List<OneOf<StatementSyntax, ArgumentMarshallerCommand>> input) {
        int blockStart = input.FindIndex(s => s.IsT1 && s.AsT1 == ArgumentMarshallerCommand.BlockStart);
        int blockEnd = input.FindLastIndex(s => s.IsT1 && s.AsT1 == ArgumentMarshallerCommand.BlockEnd);

        if (blockStart == -1 && blockEnd == -1)
            return input.Select(s => s.AsT0).ToArray();

        if (blockStart == -1 ^ blockEnd == -1)
            throw new InvalidOperationException("Unbalanced blocks detected");

        List<StatementSyntax> output = [];
        output.AddRange(input[..blockStart].Select(s => s.AsT0));

        StatementSyntax blockStatementSyntax = input[blockStart + 1].AsT0;
        switch (blockStatementSyntax) {
            case FixedStatementSyntax fixedStatementSyntax:
                List<StatementSyntax> innerBlockStatements =
                    CreateStatements(input[(blockStart + 2)..blockEnd]).ToList();

                if (fixedStatementSyntax.Statement is not EmptyStatementSyntax) {
                    innerBlockStatements.Insert(0, fixedStatementSyntax.Statement);
                }

                fixedStatementSyntax = fixedStatementSyntax.WithStatement(
                    innerBlockStatements.Count == 1 ?
                        innerBlockStatements[0] :
                        Block(innerBlockStatements)
                );
                output.Add(fixedStatementSyntax);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(blockStatementSyntax),
                    blockStatementSyntax.GetType(),
                    "Unrecognized block statement"
                );
        }

        output.AddRange(input[(blockEnd + 1)..input.Count].Select(s => s.AsT0));

        return output;
    }
}

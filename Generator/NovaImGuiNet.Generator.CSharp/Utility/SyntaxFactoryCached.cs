//#define DISABLE_CACHE

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NovaImGuiNet.Generator.CSharp;

public static class SyntaxFactoryCached {
    private static readonly Dictionary<string, StatementSyntax> ParsedStatements = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ExpressionSyntax> ParsedExpressions = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, MemberDeclarationSyntax> ParsedMemberDeclarations = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, NameSyntax> ParsedNames = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, TypeSyntax> ParsedTypeNames = new(StringComparer.Ordinal);

    private static readonly SyntaxTrivia[] LineFeedTriviaList = [SyntaxFactory.LineFeed];

    public static StatementSyntax ParseStatement(string text) =>
        GetOrAdd(text, ParsedStatements, t => SyntaxFactory.ParseStatement(t));

    public static StatementSyntax ParseStatementWithLeadingLineFeed(string text) =>
        GetOrAdd(
            text + Environment.NewLine,
            ParsedStatements,
            t => SyntaxFactory.ParseStatement(t)
        );

    public static ExpressionSyntax ParseExpression(string text) =>
        GetOrAdd(text, ParsedExpressions, t => SyntaxFactory.ParseExpression(t));

    public static MemberDeclarationSyntax ParseMemberDeclaration(string text) =>
        GetOrAdd(text, ParsedMemberDeclarations, t => SyntaxFactory.ParseMemberDeclaration(t));

    public static NameSyntax ParseName(string text) =>
        GetOrAdd(text, ParsedNames, t => SyntaxFactory.ParseName(t));

    public static TypeSyntax ParseTypeName(string text) =>
        GetOrAdd(text, ParsedTypeNames, t => SyntaxFactory.ParseTypeName(t));

    private static T GetOrAdd<T>(string text, Dictionary<string, T> dictionary, Func<string, T> factory) {
#if !DISABLE_CACHE
        lock (dictionary) {
            if (!dictionary.TryGetValue(text, out T? syntax)) {
                syntax = factory(text);
                dictionary[text] = syntax;
            }

            return syntax;
        }
#else
        return factory(text);
#endif
    }
}

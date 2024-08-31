using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NovaImGuiNet.Generator;

using static SyntaxFactory;

public static class RoslynExtensions {
    public static TSyntax AddLeadingTriviaToStart<TSyntax>(
        this TSyntax node,
        params IReadOnlyCollection<SyntaxTrivia> trivia
    ) where TSyntax : SyntaxNode {
        return
            node.WithLeadingTrivia(
                TriviaList([
                    ..trivia,
                    ..node.GetLeadingTrivia()
                ])
            );
    }

    public static TSyntax AddLeadingTriviaToEnd<TSyntax>(
        this TSyntax node,
        params IReadOnlyCollection<SyntaxTrivia> trivia
    ) where TSyntax : SyntaxNode {
        return
            node.WithLeadingTrivia(
                TriviaList([
                    ..node.GetLeadingTrivia(),
                    ..trivia
                ])
            );
    }

    public static TSyntax AddTrailingTriviaToEnd<TSyntax>(
        this TSyntax node,
        params IReadOnlyCollection<SyntaxTrivia> trivia
    ) where TSyntax : SyntaxNode {
        return
            node.WithTrailingTrivia(
                node.GetTrailingTrivia().AddRange(trivia)
            );
    }

    public static T AddDocumentationComment<T>(
        this T item,
        IReadOnlyList<string> commentLines,
        bool isXmlSummary,
        bool addToStart = false
    ) where T : CSharpSyntaxNode {
        if (commentLines.Count == 0)
            return item;

        bool singleLine = commentLines.Count == 1;

        commentLines =
            commentLines
                .Select(comment => singleLine || (!singleLine && !isXmlSummary) ? comment : "  " + comment)
                .ToArray();

        List<SyntaxTrivia> commentTrivias = [];
        if (isXmlSummary) {
            commentLines =
                commentLines
                    .Select(line => HttpUtility.HtmlEncode(line)!)
                    .ToArray();

            SyntaxTrivia lineStart = singleLine ? Whitespace("") : DocumentationCommentExterior("/// ");

            DocumentationCommentTriviaSyntax documentationCommentSyntax =
                DocumentationCommentTrivia(
                    SyntaxKind.SingleLineDocumentationCommentTrivia,
                    List<XmlNodeSyntax>([
                        XmlText().AddTextTokens(XmlTextLiteral(TriviaList(DocumentationCommentExterior("/// ")), "", "", TriviaList())),

                        XmlSummaryElement(XmlText(TokenList(
                            commentLines.SelectMany((comment, index) => new[] {
                                singleLine ?
                                    XmlTextLiteral(TriviaList(), "", "", TriviaList()) :
                                    XmlTextNewLine(TriviaList(), Environment.NewLine, Environment.NewLine, TriviaList()),

                                XmlTextLiteral(TriviaList(), comment, comment, TriviaList())
                                    .WithLeadingTrivia(lineStart),

                                !singleLine && index == commentLines.Count - 1 ?
                                    XmlTextNewLine(TriviaList(), Environment.NewLine, Environment.NewLine, TriviaList(lineStart)) :
                                    XmlTextLiteral(TriviaList(), "", "", TriviaList())
                            })
                        ))),

                        XmlText().AddTextTokens(XmlTextNewLine(TriviaList(), Environment.NewLine, Environment.NewLine, TriviaList()))
                    ]));

            commentTrivias.Add(Trivia(documentationCommentSyntax));
        } else {
            foreach (string comment in commentLines) {
                commentTrivias.AddRange([
                    Comment($"// {comment}"),
                    LineFeed
                ]);
            }
        }

        return addToStart ?
            item.AddLeadingTriviaToStart(commentTrivias) :
            item.AddLeadingTriviaToEnd(commentTrivias);
    }
}

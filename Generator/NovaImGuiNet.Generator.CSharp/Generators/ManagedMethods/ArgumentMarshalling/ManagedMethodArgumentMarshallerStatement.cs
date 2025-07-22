using OneOf;

namespace NovaImGuiNet.Generator.CSharp;

[GenerateOneOf]
public partial class ManagedMethodArgumentMarshallerStatement : OneOfBase<
    string,
    Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax, Microsoft.CodeAnalysis.SyntaxTrivia,
    ManagedMethods.ArgumentMarshallerCommand
>;

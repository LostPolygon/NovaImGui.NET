using Microsoft.CodeAnalysis.CSharp;
using OneOf;

namespace NovaImGuiNet.Generator.CSharp;

public record BindingsCompilationUnit(
    OneOf<BindingsModel.ITopLevelEntity, CompilationUnitFileInformation> EntityOrCompilationUnitFileInformation,
    CSharpSyntaxNode SyntaxNode,
    string? Category
);

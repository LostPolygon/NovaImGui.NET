using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NovaImGuiNet.Generator.CSharp;

public record CompilationUnitFileInformation(string Name, string Namespace);

public record CompilationUnitWithNamespace(CompilationUnitSyntax CompilationUnitSyntax, string Namespace);

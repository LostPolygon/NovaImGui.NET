using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NovaImGuiNet.Generator.CSharp;

public record CompilationUnitFileInformation(string Name, string Namespace, bool ForceTopLevel = false);

public record CompilationUnitWithFileInformation(CompilationUnitSyntax CompilationUnitSyntax, CompilationUnitFileInformation File);

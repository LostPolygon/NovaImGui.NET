using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NovaImGuiNet.Generator.CSharp;

public record CompilationUnitWithFileInformation(
    CompilationUnitSyntax CompilationUnitSyntax,
    CompilationUnitFileInformation File
);

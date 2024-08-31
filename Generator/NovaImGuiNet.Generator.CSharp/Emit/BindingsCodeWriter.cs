using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using OneOf;

namespace NovaImGuiNet.Generator.CSharp;

public class BindingsCodeWriter {
    public IReadOnlySet<BindingsCompilationUnit> CompilationUnits { get; }
    public BindingsContext BindingsContext { get; }

    public BindingsCodeWriter(
        IReadOnlySet<BindingsCompilationUnit> compilationUnits,
        BindingsContext bindingsContext
    ) {
        CompilationUnits = compilationUnits;
        BindingsContext = bindingsContext;
    }

    public void SaveToDirectory() {
        foreach (var grouping in CompilationUnits.GroupBy(x => CreateFileName(x.EntityOrName))) {
            if (grouping.Count() > 1) {
                throw new InvalidOperationException($"Multiple compilation units for {grouping.Key}");
            }
        }

        Parallel.ForEach(
            CompilationUnits,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            unit => SaveEntityToFile(unit.EntityOrName, unit.SyntaxNode)
        );
    }

    private void SaveEntityToFile(OneOf<BindingsModel.ITopLevelEntity, string> entityOrName, CSharpSyntaxNode syntaxNode) {
        string code = WriteCodeToString(syntaxNode, BindingsContext.Options.CodeFormatMode);
        string fileName = CreateFileName(entityOrName);
        string filePath = CreateFilePath(fileName);
        File.WriteAllText(filePath, code);
    }

    private static string CreateFileName(OneOf<BindingsModel.ITopLevelEntity, string> entityOrName) {
        string fileName = entityOrName.Match(
            entity => {
                string name = entity.Name;
                name = entity switch {
                    BindingsModel.Enum => "Enum." + name,
                    BindingsModel.Struct => "Struct." + name,
                    _ => name
                };
                return name;
            },
            name => name
        );

#if DEBUG
        fileName = $"{fileName}.cs";
#else
        fileName = $"{fileName}.gen.cs";
#endif

        return fileName;
    }

    private string CreateFilePath(string fileName) {
        return Path.Combine(BindingsContext.RootOutputPath, fileName);
    }

    private static string WriteCodeToString(SyntaxNode syntaxNode, CodeFormatMode formatMode) {
        syntaxNode = FormatNode(syntaxNode, formatMode);

        using StringWriter writer = new(CultureInfo.InvariantCulture);
        syntaxNode.WriteTo(writer);
        writer.Close();

        return writer.ToString();
    }

    private static SyntaxNode FormatNode(SyntaxNode node, CodeFormatMode formatMode) {
        if (formatMode == CodeFormatMode.Basic)
            return node.NormalizeWhitespace();

        Debug.Assert(formatMode == CodeFormatMode.Fancy);

        AdhocWorkspace workspace = new();
        OptionSet options = workspace.Options;

        void EnableOption(ref OptionSet optionSet, Option<bool> option) {
            optionSet = optionSet.WithChangedOption(option, true);
        }

        EnableOption(ref options, CSharpFormattingOptions.NewLinesForBracesInMethods);
        EnableOption(ref options, CSharpFormattingOptions.NewLinesForBracesInProperties);
        EnableOption(ref options, CSharpFormattingOptions.NewLinesForBracesInAccessors);
        EnableOption(ref options, CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody);
        EnableOption(ref options, CSharpFormattingOptions.SpaceAfterCast);
        EnableOption(ref options, CSharpFormattingOptions.IndentBlock);

        workspace.TryApplyChanges(workspace.CurrentSolution.WithOptions(options));

        SyntaxNode formattedNode = Formatter.Format(
            node,
            workspace,
            options
        );

        return formattedNode;
    }
}

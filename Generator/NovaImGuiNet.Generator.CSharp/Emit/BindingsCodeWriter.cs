using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Logging;
using MoreLinq;

namespace NovaImGuiNet.Generator.CSharp;

public class BindingsCodeWriter {
    private static readonly ILogger Log = Logging.Create<BindingsCodeWriter>();

    public IReadOnlyList<BindingsCompilationUnit> CompilationUnits { get; }
    public BindingsContext BindingsContext { get; }

    public BindingsCodeWriter(
        IReadOnlyList<BindingsCompilationUnit> compilationUnits,
        BindingsContext bindingsContext
    ) {
        CompilationUnits = compilationUnits;
        BindingsContext = bindingsContext;
    }

    public void SaveToDirectory() {
#if DEBUG
        foreach (var grouping in CompilationUnits.GroupBy(CreateFileName)) {
            if (grouping.Count() > 1) {
                throw new InvalidOperationException($"Multiple compilation units for {grouping.Key}");
            }
        }
#endif

        string[] directories =
            CompilationUnits
                .Select(CreateFileName)
                .Select(Path.GetDirectoryName)
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToArray();

        directories.ForEach(dir => Directory.CreateDirectory(CreateFilePath(dir)));

        Parallel.ForEach(
            Partitioner.Create(CompilationUnits.ToArray(), true),
            SaveEntityToFile
        );
    }

    private void SaveEntityToFile(BindingsCompilationUnit bindingsCompilationUnit) {
        string code = WriteCodeToString(bindingsCompilationUnit.SyntaxNode, BindingsContext.Options.CodeFormatMode);
        string fileName = CreateFileName(bindingsCompilationUnit);
        string filePath = CreateFilePath(fileName);
        File.WriteAllText(filePath, code);
    }

    private static string CreateFileName(BindingsCompilationUnit bindingsCompilationUnit) {
        string categorySeparator = bindingsCompilationUnit.Category == null ? "/" : $"/{bindingsCompilationUnit.Category}/";
        string fileName = bindingsCompilationUnit.EntityOrCompilationUnitFileInformation.Match(
            entity => {
                string name = entity.Name;
                switch (entity) {
                    case BindingsModel.Enum:
                    case BindingsModel.Struct:
                        if (entity.IsSpecialTopLevel) {
                            categorySeparator = "/";
                        }

                        name = $"{entity.Namespace}{categorySeparator}{entity.Name}";
                        break;
                }

                return name;
            },
            fileInfo => {
                if (fileInfo.ForceTopLevel) {
                    categorySeparator = "/";
                }

                return $"{fileInfo.Namespace}{categorySeparator}{fileInfo.Name}";
            });

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

        AdhocWorkspace workspace = GetAdhocWorkspace();

        SyntaxNode formattedNode = Formatter.Format(
            node,
            workspace
        );

        return formattedNode;
    }

    private static AdhocWorkspace GetAdhocWorkspace() {
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
        return workspace;
    }
}

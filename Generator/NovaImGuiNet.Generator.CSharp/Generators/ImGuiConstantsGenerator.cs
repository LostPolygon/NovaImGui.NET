using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CSharpCodeGenerationUtility;
using static SyntaxFactoryCached;

public class ImGuiConstantsGenerator(BindingsContext bindingsContext, MetadataDto dto) {
    public IEnumerable<Struct> Generate() {
        IEnumerable<IGrouping<string, MetadataDto.Define>> definesByFileNames =
            dto.Defines
                .GroupBy(x => x.SourceLocation.Filename);

        foreach (IGrouping<string, MetadataDto.Define> definesByFileName in definesByFileNames) {
            BindingsFileCustomizations fileCustomizations =
                bindingsContext.Customizations.RequireCustomization(definesByFileName.Key);

            Struct? constantsClass = GenerateForNamespace(
                fileCustomizations.Namespace,
                fileCustomizations.ConstantsClassName,
                definesByFileName.ToArray()
            );

            if (constantsClass != null) {
                yield return constantsClass;
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="targetNamespace"></param>
    /// <param name="className"></param>
    /// <param name="defineDtos"></param>
    /// <returns>null if the class has no members</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private Struct? GenerateForNamespace(string targetNamespace, string className, IReadOnlyList<MetadataDto.Define> defineDtos) {
        List<Field> constants = [];

        // TODO: implement generating 'public static readonly IReadOnlyList<string> CompilerDefines = [];'
        // ReSharper disable once CollectionNeverQueried.Local
        List<string> compilerDefines = [];

        foreach (MetadataDto.Define defineDto in defineDtos) {
            if (IsDefineSkipped(defineDto))
                continue;

            if (defineDto.Content == null) {
                compilerDefines.Add(defineDto.Name);
                continue;
            }

            ExpressionSyntax expressionSyntax = ParseExpression(defineDto.Content);
            if (expressionSyntax is not LiteralExpressionSyntax literalExpressionSyntax)
                continue;

            string constFieldType = literalExpressionSyntax.Kind() switch {
                SyntaxKind.StringLiteralExpression or SyntaxKind.NumericLiteralExpression =>
                    GetBuiltinTypeAlias(literalExpressionSyntax.Token.Value!.GetType())!,
                _ => throw new ArgumentOutOfRangeException($"Unexpected literal kind: {literalExpressionSyntax.Kind()}")
            };

            string constFieldValue = literalExpressionSyntax.Kind() switch {
                SyntaxKind.StringLiteralExpression or SyntaxKind.NumericLiteralExpression =>
                    literalExpressionSyntax.ToFullString(),
                _ => throw new ArgumentOutOfRangeException($"Unexpected literal kind: {literalExpressionSyntax.Kind()}")
            };

            Field field = new(
                MetadataToBindingsModelMappingUtility.Map(MetadataDtoToModelMappingUtility.Map(defineDto.Comments)),
                MetadataToBindingsModelMappingUtility.Map(MetadataDtoToModelMappingUtility.Map(defineDto.Conditionals)),
                MetadataToBindingsModelMappingUtility.Map(MetadataDtoToModelMappingUtility.Map(defineDto.SourceLocation)),
                defineDto.IsInternal,
                true,
                null
            ) {
                Name = defineDto.Name,
                Type = new Builtin(constFieldType, null, StorageClass.NotSpecified).AsEntityReference(),
                Value = constFieldValue
            };

            constants.Add(field);
        }

        if (constants.Count == 0 /*&& compilerDefines.Count == 0*/)
            return null;

        Struct structure = new() {
            Kind = Struct.StructKind.StaticClass,
            Name = className,
            Namespace = targetNamespace,
            Comments = new Comments(["Dear ImGui compilation constants"], [], []),
            IsSpecialTopLevel = true
        };

        structure.Children.AddRange(constants);
        return structure;
    }

    private static bool IsDefineSkipped(MetadataDto.Define defineDto) {
        // Those complex defines don't work in C# and don't make much sense there either
        if (defineDto.Conditionals.Any(c => c.Expression.Contains("defined(_MSC_VER)")))
            return true;

        // In .NET, this should be a 'readonly static' field instead of a const switched by compiler defines,
        // but no one is going to miss this one when Environment.NewLine exists
        if (defineDto.Name == "IM_NEWLINE")
            return true;

        return false;
    }
}

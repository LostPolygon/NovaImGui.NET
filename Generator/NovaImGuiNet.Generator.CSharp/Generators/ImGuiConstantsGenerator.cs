using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static SyntaxFactory;
using static CSharpCodeGenerationUtility;

public class ImGuiConstantsGenerator(BindingsContext bindingsContext, MetadataDto dto) {
    public Struct Generate() {
        List<Field> constants = [];

        // TODO: implement generating 'public static readonly IReadOnlyList<string> CompilerDefines = [];'
        // ReSharper disable once CollectionNeverQueried.Local
        List<string> compilerDefines = [];

        foreach (MetadataDto.Define defineDto in dto.Defines) {
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

        Struct structure = new() {
            Kind = Struct.StructKind.StaticClass,
            Name = "ImGuiConstants",
            Namespace = bindingsContext.Namespace,
            Comments = new Comments(["Dear ImGui compilation constants"], [], [])
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

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

using static CSharpCodeGenerationUtility;
using static BindingsModel;
using static SyntaxFactory;
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
    private Struct? GenerateForNamespace(
        string targetNamespace,
        string className,
        IReadOnlyList<MetadataDto.Define> defineDtos
    ) {
        List<ParseDefineResult> constants = [];
        List<MetadataDto.Define> unparsedDefines = [];

        // TODO: implement generating 'public static readonly IReadOnlyDictionary<string, string?> CompilerDefines = [];'
        // ReSharper disable once CollectionNeverQueried.Local
        List<string> flagDefines = [];

        foreach (MetadataDto.Define defineDto in defineDtos) {
            if (IsDefineSkipped(defineDto))
                continue;

            if (defineDto.Content == null) {
                flagDefines.Add(defineDto.Name);
                continue;
            }

            ParseDefineResult? parseDefineResult = ParseDefine(defineDto, false);
            if (parseDefineResult != null) {
                constants.Add(parseDefineResult);
            } else {
                unparsedDefines.Add(defineDto);
            }
        }

        foreach (MetadataDto.Define unparsedDefineDto in unparsedDefines) {
            // Try to match unparsed defines with existing parsed defines by name,
            // otherwise just add them as strings
            ParseDefineResult? matchingValueConstant = constants.FirstOrDefault(c => c.DefineDto.Name == unparsedDefineDto.Content);
            ParseDefineResult parsedDefine =
                matchingValueConstant != null ?
                    new ParseDefineResult(unparsedDefineDto, matchingValueConstant.Type, matchingValueConstant.DefineDto.Name) :
                    new ParseDefineResult(unparsedDefineDto, "string", $"\"{unparsedDefineDto.Content}\"");
            constants.Add(parsedDefine);
        }

        if (constants.Count == 0 && flagDefines.Count == 0)
            return null;

        Struct structure = new() {
            Kind = Struct.StructKind.StaticClass,
            Name = className,
            Namespace = targetNamespace,
            Comments = new Comments(["Dear ImGui compilation constants"], [], []),
            IsSpecialTopLevel = true
        };

        structure.Children.AddRange(constants.Select(CreateConstantField));

        if (flagDefines.Count != 0) {
            FieldDeclarationSyntax fieldDeclarationSyntax = CreateFlagDefinesListFieldDeclaration(flagDefines);
            structure.Children.Add(new MemberDeclarationSyntaxEntity(fieldDeclarationSyntax));
        }

        return structure;
    }

    private static ParseDefineResult? ParseDefine(MetadataDto.Define defineDto, bool allowUnknownAsString) {
        ExpressionSyntax expressionSyntax = ParseExpression(defineDto.Content);
        switch (expressionSyntax) {
            case LiteralExpressionSyntax literalExpressionSyntax:
                if (literalExpressionSyntax.Kind() is not (SyntaxKind.StringLiteralExpression or SyntaxKind.NumericLiteralExpression))
                    throw new ArgumentOutOfRangeException($"Unexpected literal expression kind: {expressionSyntax.Kind()}");

                return new ParseDefineResult(
                    defineDto,
                    GetBuiltinTypeAlias(literalExpressionSyntax.Token.Value!.GetType())!,
                    literalExpressionSyntax.ToFullString()
                );
            case PrefixUnaryExpressionSyntax prefixUnaryExpressionSyntax:
                if (prefixUnaryExpressionSyntax.Operand is not LiteralExpressionSyntax operandLiteralExpression)
                    throw new ArgumentOutOfRangeException($"Unexpected operand expression kind: {prefixUnaryExpressionSyntax.Operand.Kind()}");

                return new ParseDefineResult(
                    defineDto,
                    GetBuiltinTypeAlias(operandLiteralExpression.Token.Value!.GetType())!,
                    expressionSyntax.ToFullString()
                );
            default:
                if (!allowUnknownAsString)
                    return null;

                return new ParseDefineResult(
                    defineDto,
                    "string",
                    $"\"{defineDto.Content}\""
                );
        }
    }

    private static FieldDeclarationSyntax CreateFlagDefinesListFieldDeclaration(List<string> flagDefines) {
        VariableDeclaratorSyntax variableDeclaratorSyntax =
            VariableDeclarator("FlagCompilerDefines")
                .WithInitializer(
                    EqualsValueClause(
                        CollectionExpression(
                            SeparatedList<CollectionElementSyntax>(
                                flagDefines.Select(flag =>
                                    ExpressionElement(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(flag)))
                                )
                            )
                        )
                    )
                );
        FieldDeclarationSyntax fieldDeclarationSyntax =
            FieldDeclaration(
                    VariableDeclaration(
                        ParseTypeName("IReadOnlyList<string>"),
                        SingletonSeparatedList(variableDeclaratorSyntax)
                    )
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.ReadOnlyKeyword));
        return fieldDeclarationSyntax;
    }

    private static Field CreateConstantField(ParseDefineResult? parseDefineResult) =>
        new(
            MetadataToBindingsModelMappingUtility.Map(MetadataDtoToModelMappingUtility.Map(parseDefineResult.DefineDto.Comments)),
            MetadataToBindingsModelMappingUtility.Map(MetadataDtoToModelMappingUtility.Map(parseDefineResult.DefineDto.Conditionals)),
            MetadataToBindingsModelMappingUtility.Map(MetadataDtoToModelMappingUtility.Map(parseDefineResult.DefineDto.SourceLocation)),
            parseDefineResult.DefineDto.IsInternal,
            true,
            null
        ) {
            BaseName = parseDefineResult.DefineDto.Name,
            Type = new Builtin(parseDefineResult.Type, null, StorageClass.NotSpecified).AsEntityReference(),
            Value = parseDefineResult.Value
        };

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

    private record ParseDefineResult(MetadataDto.Define DefineDto, string Type, string Value);
}

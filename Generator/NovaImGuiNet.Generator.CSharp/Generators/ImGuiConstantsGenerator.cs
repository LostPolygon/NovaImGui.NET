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
        return GenerateConstantsClassesData().Select(data => {
            bindingsContext.NumericValueSubstitutions.AddRange(
                data.Constants
                    .Where(c => c.ValueInteger != null)
                    .Select(c => new KeyValuePair<string, long>(c.DefineDto.Name, c.ValueInteger!.Value))
            );

            return CreateConstantsClassStruct(data);
        });
    }

    private IEnumerable<ConstantsClassData> GenerateConstantsClassesData() {
        IEnumerable<IGrouping<string, MetadataDto.Define>> definesByFileNames =
            dto.Defines.GroupBy(x => x.SourceLocation.Filename);

        foreach (IGrouping<string, MetadataDto.Define> fileDefines in definesByFileNames) {
            BindingsFileCustomizations fileCustomizations =
                bindingsContext.Customizations.RequireCustomization(fileDefines.Key);

            ParseDefinesResult parseDefinesResult = ParseDefines(fileDefines.ToArray());
            if (parseDefinesResult.IsEmpty)
                continue;

            yield return new ConstantsClassData(
                fileCustomizations.Namespace,
                fileCustomizations.ConstantsClassName,
                parseDefinesResult.Constants,
                parseDefinesResult.FlagDefines
            );
        }
    }

    private static Struct CreateConstantsClassStruct(ConstantsClassData classData) {
        Struct structure = new() {
            Kind = Struct.StructKind.StaticClass,
            Name = classData.Name,
            Namespace = classData.Namespace,
            Comments = new Comments(["Dear ImGui compilation constants"], [], []),
            IsSpecialTopLevel = true
        };

        structure.Children.AddRange(classData.Constants.Select(CreateConstantField));

        if (classData.FlagDefines.Count != 0) {
            FieldDeclarationSyntax fieldDeclarationSyntax = CreateFlagDefinesListFieldDeclaration(classData.FlagDefines);
            structure.Children.Add(new MemberDeclarationSyntaxEntity(fieldDeclarationSyntax));
        }

        return structure;
    }

    /// <param name="defineDtos"></param>
    /// <returns>null if the class has no members</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private ParseDefinesResult ParseDefines(IReadOnlyList<MetadataDto.Define> defineDtos) {
        List<ParsedDefine> constants = [];
        List<MetadataDto.Define> unparsedDefines = [];
        List<string> flagDefines = [];

        foreach (MetadataDto.Define defineDto in defineDtos) {
            if (defineDto.Content == null) {
                flagDefines.Add(defineDto.Name);
                continue;
            }

            ParsedDefine? parsedDefine = ParseDefine(defineDto, false);
            if (parsedDefine != null) {
                constants.Add(parsedDefine);
            } else {
                unparsedDefines.Add(defineDto);
            }
        }

        foreach (MetadataDto.Define unparsedDefineDto in unparsedDefines) {
            // Try to match unparsed defines with existing parsed defines by name,
            // otherwise just add them as strings
            ParsedDefine? matchingValueConstant = constants.FirstOrDefault(c => c.DefineDto.Name == unparsedDefineDto.Content);
            ParsedDefine parsedDefine =
                matchingValueConstant != null ?
                    matchingValueConstant with { DefineDto = unparsedDefineDto, Value = matchingValueConstant.DefineDto.Name } :
                    new ParsedDefine(unparsedDefineDto, "string", $"\"{unparsedDefineDto.Content}\"", null);
            constants.Add(parsedDefine);
        }

        return new ParseDefinesResult(constants, flagDefines);
    }

    private static ParsedDefine? ParseDefine(MetadataDto.Define defineDto, bool allowUnknownAsString) {
        ExpressionSyntax expressionSyntax = ParseExpression(defineDto.Content!);
        switch (expressionSyntax) {
            case LiteralExpressionSyntax literalExpressionSyntax:
                if (literalExpressionSyntax.Kind() is not (SyntaxKind.StringLiteralExpression or SyntaxKind.NumericLiteralExpression))
                    throw new ArgumentOutOfRangeException($"Unexpected literal expression kind: {expressionSyntax.Kind()}");

                return new ParsedDefine(
                    defineDto,
                    GetBuiltinTypeAlias(literalExpressionSyntax.Token.Value!.GetType())!,
                    literalExpressionSyntax.ToFullString(),
                    literalExpressionSyntax.Kind() == SyntaxKind.NumericLiteralExpression ?
                        MathUtility.EvaluateExpressionAsLong(literalExpressionSyntax.ToFullString()) :
                        null
                );
            case PrefixUnaryExpressionSyntax prefixUnaryExpressionSyntax:
                if (prefixUnaryExpressionSyntax.Operand is not LiteralExpressionSyntax operandLiteralExpression)
                    throw new ArgumentOutOfRangeException($"Unexpected operand expression kind: {prefixUnaryExpressionSyntax.Operand.Kind()}");

                return new ParsedDefine(
                    defineDto,
                    GetBuiltinTypeAlias(operandLiteralExpression.Token.Value!.GetType())!,
                    expressionSyntax.ToFullString(),
                    MathUtility.EvaluateExpressionAsLong(expressionSyntax.ToFullString())
                );
            default:
                if (!allowUnknownAsString)
                    return null;

                return new ParsedDefine(
                    defineDto,
                    "string",
                    $"\"{defineDto.Content}\"",
                    null
                );
        }
    }

    private static FieldDeclarationSyntax CreateFlagDefinesListFieldDeclaration(IReadOnlyList<string> flagDefines) {
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

    private static Field CreateConstantField(ParsedDefine parsedDefine) =>
        new(
            MetadataToBindingsModelMappingUtility.Map(MetadataDtoToModelMappingUtility.Map(parsedDefine.DefineDto.Comments)),
            MetadataToBindingsModelMappingUtility.Map(MetadataDtoToModelMappingUtility.Map(parsedDefine.DefineDto.Conditionals)),
            MetadataToBindingsModelMappingUtility.Map(MetadataDtoToModelMappingUtility.Map(parsedDefine.DefineDto.SourceLocation)),
            parsedDefine.DefineDto.IsInternal,
            true,
            null
        ) {
            BaseName = parsedDefine.DefineDto.Name,
            Type = new Builtin(parsedDefine.Type, null, StorageClass.NotSpecified).AsEntityReference(),
            Value = parsedDefine.Value,
            IsDisabled = IsDefineCompatibleWithCSharp(parsedDefine.DefineDto)
        };

    private static bool IsDefineCompatibleWithCSharp(MetadataDto.Define defineDto) {
        // In .NET, this should be a 'readonly static' field instead of a const switched by compiler defines,
        // but no one is going to miss this one when Environment.NewLine exists
        if (defineDto.Name == "IM_NEWLINE")
            return true;

        return false;
    }

    public record ConstantsClassData(
        string Namespace,
        string Name,
        IReadOnlyList<ParsedDefine> Constants,
        IReadOnlyList<string> FlagDefines
    ) {
        public bool IsEmpty => Constants.Count == 0 && FlagDefines.Count == 0;
    }

    public record ParsedDefine(MetadataDto.Define DefineDto, string Type, string Value, long? ValueInteger);

    private record ParseDefinesResult(IReadOnlyList<ParsedDefine> Constants, IReadOnlyList<string> FlagDefines) {
        public bool IsEmpty => Constants.Count == 0 && FlagDefines.Count == 0;
    }
}

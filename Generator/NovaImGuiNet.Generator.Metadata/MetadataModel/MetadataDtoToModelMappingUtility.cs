namespace NovaImGuiNet.Generator.Metadata;

public static class MetadataDtoToModelMappingUtility {
    public static MetadataModel.Enum Map(MetadataDto.Enumeration dto) {
        List<MetadataModel.Enum.Member> members = [];
        foreach (MetadataDto.Enumeration.Member enumDtoElement in dto.Elements) {
            members.Add(new MetadataModel.Enum.Member(
                enumDtoElement.Name,
                enumDtoElement.ValueExpression,
                enumDtoElement.Value,
                Map(enumDtoElement.Comments),
                Map(enumDtoElement.Conditionals).ToEqualityList(),
                Map(enumDtoElement.SourceLocation),
                enumDtoElement.IsInternal
            ));
        }

        MetadataModel.Enum model = new(
            dto.Name,
            Map(dto.Comments),
            Array.Empty<MetadataModel.Conditional>().ToEqualityList(),
            Map(dto.SourceLocation),
            dto.IsInternal,
            members,
            dto.IsFlagsEnum
        );

        return model;
    }

    public static MetadataModel.Comments Map(MetadataDto.Comments dto) {
        return new MetadataModel.Comments(
            new EqualityList<string>(dto.Preceding.Select(TransformLine)),
            dto.Attached != null ? [TransformLine(dto.Attached)] : []
        );

        static string TransformLine(string line) =>
            line
                .RemovePrefix("// ")
                .RemovePrefix("//")
                .Trim();
    }

    public static IReadOnlyList<MetadataModel.Conditional> Map(MetadataDto.Conditional[] dto) =>
        dto
            .Select(c => new MetadataModel.Conditional(c.Condition, c.Expression))
            .ToEqualityList();

    public static MetadataModel.SourceLocation? Map(MetadataDto.SourceLocation? dto) =>
        dto != null ? new(dto.Value.Filename, dto.Value.Line) : null;

    public static MetadataModel.Builtin Map(MetadataDto.TypeValue.TypeDescriptionBuiltin dto) =>
        new(
            dto.BuiltinType,
            (dto.StorageClasses ?? []).FirstOrDefault() switch {
                MetadataDto.StorageClass.NotSpecified => MetadataModel.StorageClass.NotSpecified,
                MetadataDto.StorageClass.Const => MetadataModel.StorageClass.Const,
                _ => throw new ArgumentOutOfRangeException()
            }
        );
}

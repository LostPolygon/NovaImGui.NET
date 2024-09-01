using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

public partial class BindingsModelFromMetadataResolver {
    private BindingsModel.Enum ResolveEnum(MetadataModel.Enum metadata) =>
        new(
            MetadataToBindingsModelMappingUtility.Map(metadata.Comments),
            MetadataToBindingsModelMappingUtility.Map(metadata.Conditionals),
            metadata.IsInternal,
            metadata.IsFlagsEnum
        ) {
            Name = metadata.Name,
            Namespace = bindingsContext.Namespace,
            Members = metadata.Members
                .Select(member =>
                    new BindingsModel.Enum.Member(
                        MetadataToBindingsModelMappingUtility.Map(member.Comments),
                        MetadataToBindingsModelMappingUtility.Map(member.Conditionals),
                        metadata.IsInternal
                    ) {
                        Name = member.Name,
                        Value = member.Value,
                        ValueNumeric = member.ValueNumeric,
                    }
                )
                .ToList()
        };

    private BindingsModel.Typedef ResolveTypedef(MetadataModel.IEntityReference<MetadataModel.Typedef> metadata) =>
        new(
            MetadataToBindingsModelMappingUtility.Map(metadata.Value.Comments),
            MetadataToBindingsModelMappingUtility.Map(metadata.Value.Conditionals),
            metadata.Value.IsInternal
        ) {
            Name = metadata.Value.Name,
            Namespace = bindingsContext.Namespace,
            Type = ResolveEntity(metadata.Value.Type)
        };

    private BindingsModel.Struct ResolveStruct(MetadataModel.IEntityReference<MetadataModel.Struct> metadata) =>
        new(
            MetadataToBindingsModelMappingUtility.Map(metadata.Value.Comments),
            MetadataToBindingsModelMappingUtility.Map(metadata.Value.Conditionals),
            metadata.Value.IsInternal,
            metadata.Value.ByValue,
            [],
            [
                //..metadata.Value.Delegates.Select(ResolveEntity).Cast<BindingsModel.IEntityReference<BindingsModel.Delegate>>(),
                ..metadata.Value.Fields.Select(ResolveStructField)
            ]
        ) {
            Name = metadata.Value.Name,
            Namespace = bindingsContext.Namespace,
            Kind = metadata.Value.Kind switch {
                MetadataModel.Struct.StructKind.Struct => BindingsModel.Struct.StructKind.Struct,
                MetadataModel.Struct.StructKind.Union => BindingsModel.Struct.StructKind.Union,
                MetadataModel.Struct.StructKind.StaticClass => BindingsModel.Struct.StructKind.StaticClass,
                _ => throw new ArgumentOutOfRangeException()
            }
        };

    private BindingsModel.Field ResolveStructField(MetadataModel.Field metadata) =>
        new(
            MetadataToBindingsModelMappingUtility.Map(metadata.Comments),
            MetadataToBindingsModelMappingUtility.Map(metadata.Conditionals),
            metadata.IsInternal,
            metadata.IsConstant,
            metadata.Width
        ) {
            Name = metadata.Name,
            Value = metadata.Value,
            Type = ResolveEntity(metadata.Type),
        };

    private BindingsModel.Function ResolveFunction(MetadataModel.IEntityReference<MetadataModel.Function> metadata) =>
        new(
            MetadataToBindingsModelMappingUtility.Map(metadata.Value.Comments),
            MetadataToBindingsModelMappingUtility.Map(metadata.Value.Conditionals),
            metadata.Value.IsInternal,
            metadata.Value.OriginalClass != null ?
                new BindingsModel.EntityReferenceWrapper<BindingsModel.Struct>(ResolveEntity(metadata.Value.OriginalClass)) :
                null,
            metadata.Value.OriginalFullyQualifiedName,
            metadata.Value.IsDefaultArgumentHelper,
            metadata.Value.IsImstrHelper,
            metadata.Value.HasImstrHelper,
            metadata.Value.IsManualHelper,
            metadata.Value.IsUnformattedHelper
        ) {
            Name = metadata.Value.Name,
            Arguments = metadata.Value.Arguments.Select(ResolveFunctionArgument).ToList(),
            ReturnType = ResolveEntity(metadata.Value.ReturnType)
        };

    private BindingsModel.Delegate ResolveDelegate(MetadataModel.IEntityReference<MetadataModel.Delegate> metadata) =>
        new(
            MetadataToBindingsModelMappingUtility.Map(metadata.Value.Comments),
            MetadataToBindingsModelMappingUtility.Map(metadata.Value.Conditionals),
            metadata.Value.IsInternal
        ) {
            Name = metadata.Value.Name,
            Arguments = metadata.Value.Arguments.Select(ResolveFunctionArgument).ToList(),
            ReturnType = ResolveEntity(metadata.Value.ReturnType)
        };

    private BindingsModel.FunctionArgument ResolveFunctionArgument(MetadataModel.FunctionArgument metadata) =>
        new() {
            Name = metadata.Name,
            Type = ResolveEntity(metadata.Type),
            ArrayBounds = metadata.ArrayBounds,
            DeclarationDefaultValue = metadata.DefaultValue,
            IsVarargs = metadata.IsVarargs,
            IsInstancePointer = metadata.IsInstancePointer,
        };

    private BindingsModel.Pointer ResolvePointer(MetadataModel.IEntityReference<MetadataModel.Pointer> metadata) =>
        new(
            ResolveEntity(metadata.Value.ValueType),
            metadata.Value.IsNullable
        );

    private BindingsModel.Array ResolveArray(MetadataModel.IEntityReference<MetadataModel.Array> metadata) =>
        new(
            ResolveEntity(metadata.Value.ElementType),
            metadata.Value.ArrayBounds,
            metadata.Value.ArrayBoundsNumeric
        );

    private BindingsModel.Builtin ResolveBuiltinType(MetadataModel.Builtin metadata) {
        (string BuiltinName, Type? Type) baseType = metadata.Type switch {
            "int" => ("int", typeof(int)),
            "short" => ("short", typeof(short)),
            "long_long" => ("long", typeof(long)),
            "unsigned_long_long" => ("ulong", typeof(ulong)),
            "unsigned_int" => ("uint", typeof(uint)),
            "unsigned_short" => ("ushort", typeof(ushort)),
            "char" => ("byte", typeof(byte)), //"char" => ("/* signed */ byte", typeof(byte)),
            "unsigned_char" => ("byte", typeof(byte)),
            "float" => ("float", typeof(float)),
            "double" => ("double", typeof(double)),
            "bool" => ("bool", typeof(bool)),
            "void" => ("void", typeof(void)),
            _ => (null!, null)
        };

        if (baseType.BuiltinName == null!) {
            // Subscriber can modify the entity or throw an error
            UnresolvedTypeDetected?.Invoke(metadata);

            baseType.BuiltinName = metadata.Type;
        }

        return new BindingsModel.Builtin(
            baseType.BuiltinName,
            baseType.Type,
            (BindingsModel.StorageClass) metadata.StorageClass
        );
    }
}

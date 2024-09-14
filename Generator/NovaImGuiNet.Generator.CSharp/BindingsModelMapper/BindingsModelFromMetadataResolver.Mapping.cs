using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

using static MetadataToBindingsModelMappingUtility;

public partial class BindingsModelFromMetadataResolver {
    private BindingsModel.Enum ResolveEnum(MetadataModel.Enum metadata) =>
        new(
            Map(metadata.Comments),
            Map(metadata.Conditionals),
            Map(metadata.SourceLocation),
            metadata.IsInternal,
            metadata.IsFlagsEnum
        ) {
            Name = metadata.Name,
            Members = metadata.Members
                .Select(member =>
                    new BindingsModel.Enum.Member(
                        Map(member.Comments),
                        Map(member.Conditionals),
                        Map(metadata.SourceLocation),
                        metadata.IsInternal
                    ) {
                        Name = member.Name,
                        Value = member.Value,
                        ValueNumeric = member.ValueNumeric,
                    }
                )
                .ToList()
        };

    private BindingsModel.Typedef ResolveTypedef(MetadataModel.Typedef metadata) =>
        new(
            Map(metadata.Comments),
            Map(metadata.Conditionals),
            Map(metadata.SourceLocation),
            metadata.IsInternal
        ) {
            Name = metadata.Name,
            Type = ResolveEntity(metadata.Type)
        };

    private BindingsModel.Struct ResolveStruct(MetadataModel.Struct metadata) =>
        new(
            Map(metadata.Comments),
            Map(metadata.Conditionals),
            Map(metadata.SourceLocation),
            metadata.IsInternal,
            metadata.ByValue,
            [],
            [
                //..metadata.Delegates.Select(ResolveEntity).Cast<BindingsModel.IEntityReference<BindingsModel.Delegate>>(),
                ..metadata.Fields.Select(ResolveStructField)
            ]
        ) {
            Name = metadata.Name,
            Kind = metadata.Kind switch {
                MetadataModel.Struct.StructKind.Struct => BindingsModel.Struct.StructKind.Struct,
                MetadataModel.Struct.StructKind.Union => BindingsModel.Struct.StructKind.Union,
                MetadataModel.Struct.StructKind.StaticClass => BindingsModel.Struct.StructKind.StaticClass,
                _ => throw new ArgumentOutOfRangeException()
            }
        };

    private BindingsModel.Field ResolveStructField(MetadataModel.Field metadata) =>
        new(
            Map(metadata.Comments),
            Map(metadata.Conditionals),
            Map(metadata.SourceLocation),
            metadata.IsInternal,
            metadata.IsConstant,
            metadata.Width
        ) {
            BaseName = metadata.Name,
            Value = metadata.DefaultValue,
            Type = ResolveEntity(metadata.Type),
        };

    private BindingsModel.Function ResolveFunction(MetadataModel.Function metadata) {
        BindingsModel.EntityReferenceWrapper<BindingsModel.Struct>? originalClass =
            metadata.OriginalClass != null ?
                new BindingsModel.EntityReferenceWrapper<BindingsModel.Struct>(ResolveEntity(metadata.OriginalClass)) :
                null;

        return new(
            Map(metadata.Comments),
            Map(metadata.Conditionals),
            Map(metadata.SourceLocation),
            metadata.IsInternal,
            originalClass,
            metadata.OriginalFullyQualifiedName,
            metadata.IsDefaultArgumentHelper,
            metadata.IsImstrHelper,
            metadata.HasImstrHelper,
            metadata.IsManualHelper,
            metadata.IsUnformattedHelper,
            metadata.IsStatic,
            originalClass != null && originalClass.Value.Name == metadata.OriginalFullyQualifiedName
        ) {
            Name = metadata.Name,
            Arguments = metadata.Arguments.Select(ResolveFunctionArgument).ToList(),
            ReturnType = ResolveEntity(metadata.ReturnType),
            IsNativeFunction = true
        };
    }

    private BindingsModel.Delegate ResolveDelegate(MetadataModel.Delegate metadata) =>
        new(
            Map(metadata.Comments),
            Map(metadata.Conditionals),
            Map(metadata.SourceLocation),
            metadata.IsInternal
        ) {
            Name = metadata.Name,
            Arguments = metadata.Arguments.Select(ResolveFunctionArgument).ToList(),
            ReturnType = ResolveEntity(metadata.ReturnType)
        };

    private BindingsModel.FunctionArgument ResolveFunctionArgument(MetadataModel.FunctionArgument metadata) =>
        new() {
            Name = metadata.Name,
            Type = ResolveEntity(metadata.Type),
            ArrayBounds = metadata.ArrayBounds,
            DefaultValue = metadata.DefaultValue,
            IsVarargs = metadata.IsVarargs,
            IsInstancePointer = metadata.IsInstancePointer,
            PassType = BindingsModel.FunctionArgumentPassType.Normal
        };

    private BindingsModel.Entity ResolvePointer(MetadataModel.Pointer metadata) {
        // Special-case const char*
        bool isConstCharPointer =
            metadata.ValueType.Value is MetadataModel.Builtin {
                Type: "char",
                StorageClass: MetadataModel.StorageClass.Const
            };

        BindingsModel.IEntityReference valueType = ResolveEntity(metadata.ValueType);
        if (isConstCharPointer && valueType.Value is BindingsModel.Builtin builtin) {
            builtin.IsConstCharPointer = true;
        }

        return new BindingsModel.Pointer(
            valueType,
            metadata.IsNullable
        );
    }

    private BindingsModel.Array ResolveArray(MetadataModel.Array metadata) =>
        new(
            ResolveEntity(metadata.ElementType),
            metadata.ArrayBounds,
            metadata.ArrayBoundsNumeric ?? MathUtility.EvaluateExpressionAsLong(metadata.ArrayBounds)
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

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

public class MetadataToBindingsModelMapper {
    private static readonly ILogger Log = Logging.Create<MetadataToBindingsModelMapper>();

    private MetadataModel Metadata { get; }
    private BindingsContext BindingsContext { get; }

    private Dictionary<MetadataModel.IEntityReference, BindingsModel.IEntityReference> MetadataEntityToBindingsEntity { get; } = new();

    public event Action<MetadataModel.Entity>? UnresolvedTypeDetected;

    public MetadataToBindingsModelMapper(MetadataModel metadata, BindingsContext bindingsContext) {
        Metadata = metadata;
        BindingsContext = bindingsContext;
    }

    public BindingsModel Map() {
        ResolveAllModelEntities();

        BindingsModel bindingsModel = new() {
            Enums = GetModels<BindingsModel.IEntityReference<BindingsModel.Enum>>(),
            Structs = GetModels<BindingsModel.IEntityReference<BindingsModel.Struct>>(),
            Typedefs = GetModels<BindingsModel.IEntityReference<BindingsModel.Typedef>>(),
            FreeFunctions = GetModels<BindingsModel.IEntityReference<BindingsModel.Function>>()
        };

        return bindingsModel;

        List<TEntityValue> GetModels<TEntityValue>() {
            return
                MetadataEntityToBindingsEntity
                    .Values
                    .OfType<TEntityValue>()
                    .ToList();
        }
    }

    private void ResolveAllModelEntities() {
        foreach (var entityReference in Metadata.Enums) {
            ResolveEntity(entityReference);
        }

        foreach (var entityReference in Metadata.Typedefs) {
            ResolveEntity(entityReference);
        }

        foreach (var entityReference in Metadata.Structs) {
            ResolveEntity(entityReference);
            foreach (var function in entityReference.Value.Functions) {
                ResolveEntity(function);
            }
        }

        foreach (var entityReference in Metadata.FreeFunctions) {
            ResolveEntity(entityReference);
        }
    }

    private BindingsModel.Enum MapEnum(MetadataModel.Enum metadata) =>
        new(
            MetadataToBindingsModelMappingUtility.Map(metadata.Comments),
            MetadataToBindingsModelMappingUtility.Map(metadata.Conditionals),
            metadata.IsInternal,
            metadata.IsFlagsEnum
        ) {
            Name = metadata.Name,
            Namespace = BindingsContext.Namespace,
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

    private BindingsModel.FunctionArgument MapFunctionArgument(MetadataModel.FunctionArgument metadata)
        => new() {
            Name = metadata.Name,
            Type = ResolveEntity(metadata.Type),
            ArrayBounds = metadata.ArrayBounds,
            DefaultValue = metadata.DefaultValue,
            IsVarargs = metadata.IsVarargs,
            IsInstancePointer = metadata.IsInstancePointer,
        };

    private BindingsModel.Field ConvertField(MetadataModel.Field metadata)
        => new(
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

    private BindingsModel.Entity ConvertEntity(MetadataModel.IEntityReference metadataRaw) {
        switch (metadataRaw) {
            case MetadataModel.IEntityReference<MetadataModel.Enum> metadata:
                return MapEnum(metadata.Value);
            case MetadataModel.IEntityReference<MetadataModel.Array> metadata:
                return ResolveArray(metadata);
            case MetadataModel.IEntityReference<MetadataModel.Builtin> metadata:
                return MapBuiltinType(metadata.Value);
            case MetadataModel.IEntityReference<MetadataModel.Pointer> metadata:
                return
                    new BindingsModel.Pointer(
                        ResolveEntity(metadata.Value.ValueType),
                        metadata.Value.IsNullable
                    );
            case MetadataModel.IEntityReference<MetadataModel.Delegate> metadata:
                return
                    new BindingsModel.Delegate(
                        MetadataToBindingsModelMappingUtility.Map(metadata.Value.Comments),
                        MetadataToBindingsModelMappingUtility.Map(metadata.Value.Conditionals),
                        metadata.Value.IsInternal
                    ) {
                        Name = metadata.Value.Name,
                        Arguments = metadata.Value.Arguments.Select(MapFunctionArgument).ToEqualityList(),
                        ReturnType = ResolveEntity(metadata.Value.ReturnType)
                    };
            case MetadataModel.IEntityReference<MetadataModel.Function> metadata:
                return MapFunction(metadata);
            case MetadataModel.IEntityReference<MetadataModel.Struct> metadata:
                return
                    new BindingsModel.Struct(
                        MetadataToBindingsModelMappingUtility.Map(metadata.Value.Comments),
                        MetadataToBindingsModelMappingUtility.Map(metadata.Value.Conditionals),
                        metadata.Value.IsInternal,
                        metadata.Value.ByValue,
                        [],
                        [
                            //..metadata.Value.Delegates.Select(ResolveEntity).Cast<BindingsModel.IEntityReference<BindingsModel.Delegate>>(),
                            ..metadata.Value.Fields.Select(ConvertField)
                        ]
                    ) {
                        Name = metadata.Value.Name,
                        Namespace = BindingsContext.Namespace,
                        Kind = metadata.Value.Kind switch {
                            MetadataModel.Struct.StructKind.Struct => BindingsModel.Struct.StructKind.Struct,
                            MetadataModel.Struct.StructKind.Union => BindingsModel.Struct.StructKind.Union,
                            MetadataModel.Struct.StructKind.StaticClass => BindingsModel.Struct.StructKind.StaticClass,
                            _ => throw new ArgumentOutOfRangeException()
                        }
                    };
            case MetadataModel.IEntityReference<MetadataModel.Typedef> metadata:
                return MapTypedef(metadata);
            default:
                throw new ArgumentOutOfRangeException(nameof(metadataRaw));
        }
    }

    private BindingsModel.Array ResolveArray(MetadataModel.IEntityReference<MetadataModel.Array> metadata) {
        return new BindingsModel.Array(
            ResolveEntity(metadata.Value.ElementType),
            metadata.Value.ArrayBounds,
            metadata.Value.ArrayBoundsNumeric
        );
    }

    private BindingsModel.Typedef MapTypedef(MetadataModel.IEntityReference<MetadataModel.Typedef> metadata) =>
        new(
            MetadataToBindingsModelMappingUtility.Map(metadata.Value.Comments),
            MetadataToBindingsModelMappingUtility.Map(metadata.Value.Conditionals),
            metadata.Value.IsInternal
        ) {
            Name = metadata.Value.Name,
            Namespace = BindingsContext.Namespace,
            Type = ResolveEntity(metadata.Value.Type)
        };

    private BindingsModel.Function MapFunction(MetadataModel.IEntityReference<MetadataModel.Function> metadata) {
        return
            new BindingsModel.Function(
                MetadataToBindingsModelMappingUtility.Map(metadata.Value.Comments),
                MetadataToBindingsModelMappingUtility.Map(metadata.Value.Conditionals),
                metadata.Value.IsInternal,
                metadata.Value.OriginalClass != null ?
                    (BindingsModel.IEntityReference<BindingsModel.Struct>) ResolveEntity(metadata.Value.OriginalClass) :
                    null,
                metadata.Value.OriginalFullyQualifiedName,
                metadata.Value.IsDefaultArgumentHelper,
                metadata.Value.IsImstrHelper,
                metadata.Value.HasImstrHelper,
                metadata.Value.IsManualHelper,
                metadata.Value.IsUnformattedHelper
            ) {
                Name = metadata.Value.Name,
                Arguments = metadata.Value.Arguments.Select(MapFunctionArgument).ToEqualityList(),
                ReturnType = ResolveEntity(metadata.Value.ReturnType)
            };
    }

    private BindingsModel.IEntityReference ResolveEntity(
        MetadataModel.IEntityReference metadata
    ) {
        ArgumentNullException.ThrowIfNull(metadata);

        if (MetadataEntityToBindingsEntity.TryGetValue(metadata, out BindingsModel.IEntityReference? bindingsEntityRaw)) {
            return bindingsEntityRaw;
        }

        BindingsModel.IEntityReference bindingsReference = CreateEmptyBindingsEntityReferenceForMetadata(metadata.Value.GetType());
        bool added = MetadataEntityToBindingsEntity.TryAdd(metadata, bindingsReference);
        Debug.Assert(added);

        // Convert the DTO to model entity and assign it to the reference
        BindingsModel.Entity userEntity = ConvertEntity(metadata);
        bindingsReference.Value = userEntity;

        return bindingsReference;
    }

    private BindingsModel.Builtin MapBuiltinType(MetadataModel.Builtin metadata) {
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

    private static BindingsModel.IEntityReference CreateEmptyBindingsEntityReferenceForMetadata(Type t) =>
        t switch {
            _ when t == typeof(MetadataModel.Array) => new BindingsModel.EntityReference<BindingsModel.Array>(),
            _ when t == typeof(MetadataModel.Builtin) => new BindingsModel.EntityReference<BindingsModel.Builtin>(),
            _ when t == typeof(MetadataModel.Delegate) => new BindingsModel.EntityReference<BindingsModel.Delegate>(),
            _ when t == typeof(MetadataModel.Enum) => new BindingsModel.EntityReference<BindingsModel.Enum>(),
            _ when t == typeof(MetadataModel.Function) => new BindingsModel.EntityReference<BindingsModel.Function>(),
            _ when t == typeof(MetadataModel.Pointer) => new BindingsModel.EntityReference<BindingsModel.Pointer>(),
            _ when t == typeof(MetadataModel.Struct) => new BindingsModel.EntityReference<BindingsModel.Struct>(),
            _ when t == typeof(MetadataModel.Typedef) => new BindingsModel.EntityReference<BindingsModel.Typedef>(),
            _ => throw new ArgumentOutOfRangeException(nameof(t), t, $"Unexpected type {t}")
        };
}

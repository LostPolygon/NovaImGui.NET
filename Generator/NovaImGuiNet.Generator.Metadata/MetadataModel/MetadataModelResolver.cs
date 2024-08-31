using System.Diagnostics;

namespace NovaImGuiNet.Generator.Metadata;

/// <summary>
/// Takes the metadata DTO and generates a model of connected metadata entities.
/// </summary>
public partial class MetadataModelResolver {
    private MetadataDto Dto { get; }
    private Dictionary<string, object> DtoNameToDtoEntity { get; } = new();

    private Dictionary<string, MetadataModel.IEntityReference> DtoNameToModelEntity { get; } = new();

    /// <summary>
    /// Generates a connected graph of all the API entities.
    /// </summary>
    public MetadataModelResolver(MetadataDto dto) {
        Dto = dto;
    }

    public MetadataModel Map() {
        DtoNameToDtoEntity.Clear();
        DtoNameToModelEntity.Clear();

        // Visit all DTO types and create a name->raw DTO map
        ResolveAllDtoEntities();

        // Using the name->raw DTO map, link all the model entities up together
        HashSet<MetadataModel.IEntityReference> allModelEntities = ResolveAllModelEntities();

        MetadataModel metadataModel = new(
            GetModels<MetadataModel.IEntityReference<MetadataModel.Enum>>(),
            GetModels<MetadataModel.IEntityReference<MetadataModel.Struct>>(),
            GetModels<MetadataModel.IEntityReference<MetadataModel.Typedef>>(),
            GetModels<MetadataModel.IEntityReference<MetadataModel.Function>>(f => f.Value.OriginalClass == null)
        );

        return metadataModel;

        IReadOnlyList<TEntityValue> GetModels<TEntityValue>(Func<TEntityValue, bool>? filter = null) {
            filter ??= _ => true;

            return
                allModelEntities
                    .OfType<TEntityValue>()
                    .Where(filter)
                    .ToArray();
        }
    }

    private HashSet<MetadataModel.IEntityReference> ResolveAllModelEntities() {
        foreach (MetadataDto.Enumeration enumerationDto in Dto.Enums) {
            ResolveEnum(enumerationDto);
        }

        foreach (MetadataDto.Struct structureDto in Dto.Structs) {
            ResolveStruct(structureDto);
        }

        foreach (MetadataDto.Function functionDto in Dto.Functions) {
            ResolveFunction(functionDto);
        }

        ResolveClassFunctions();

        HashSet<MetadataModel.IEntityReference> allModelEntities = new(DtoNameToModelEntity.Values);
        Debug.Assert(allModelEntities.Count == DtoNameToModelEntity.Count);

        foreach (MetadataDto.Typedef typedefDto in Dto.Typedefs) {
            MetadataModel.Typedef typedefValue = MapTypedef(typedefDto);
            MetadataModel.IEntityReference<MetadataModel.Typedef> typedefEntity =
                typedefValue.AsEntityReference();

            allModelEntities.Add(typedefEntity);
        }

        /*allModelEntities.UnionWith(
            Dto.Typedefs
                .Select(ResolveTypedef)
        );*/

        return allModelEntities;
    }

    private void ResolveClassFunctions() {
        MetadataModel.IEntityReference<MetadataModel.Function>[] allFunctions =
            DtoNameToModelEntity.Values
                .OfType<MetadataModel.IEntityReference<MetadataModel.Function>>()
                .ToArray();

        var functionsByStruct =
            allFunctions
                .GroupBy(x => x.Value.OriginalClass)
                .Where(group => group.Key != null)
                .ToArray();

        foreach (var group in functionsByStruct) {
            MetadataModel.IEntityReference<MetadataModel.Struct> structure = group.Key!;
            MetadataModel.IEntityReference<MetadataModel.Function>[] structureFunctions = group.ToArray();

            structure.Value = structure.Value with {
                Functions = structureFunctions
            };
        }
    }

    private MetadataModel.IEntityReference ResolveTypeValue(MetadataDto.TypeValue typeValue) {
        return ResolveTypeDescription(
            typeValue.Description,
            typeValue.TypeDetails as MetadataDto.TypeValue.TypeDetailsFunctionPointer
        );
    }

    private MetadataModel.IEntityReference ResolveTypeDescription(
        MetadataDto.TypeValue.TypeDescription typeDescription,
        MetadataDto.TypeValue.ITypeDetails? typeDetails
    ) {
        switch (typeDescription) {
            case MetadataDto.TypeValue.TypeDescriptionArray typeDescriptionArray:
                return new MetadataModel.Array(
                    ResolveTypeDescription(typeDescriptionArray.InnerType, null)!,
                    typeDescriptionArray.Bounds,
                    null
                ).AsEntityReference();
            case MetadataDto.TypeValue.TypeDescriptionBuiltin typeDescriptionBuiltin:
                return MetadataDtoToModelMappingUtility.Map(typeDescriptionBuiltin).AsEntityReference();
            case MetadataDto.TypeValue.TypeDescriptionFunction typeDescriptionFunction:
                /*HandleTypeDescription(typeDescriptionFunction.ReturnType);
                foreach (Dto.TypeValue.TypeDescription parameter in typeDescriptionFunction.Parameters) {
                    HandleTypeDescription(parameter);
                }*/

                break;
            case MetadataDto.TypeValue.TypeDescriptionPointer typeDescriptionPointer:
                return new MetadataModel.Pointer(
                    ResolveTypeDescription(typeDescriptionPointer.InnerType, null)!,
                    typeDescriptionPointer.IsNullable
                ).AsEntityReference();
            case MetadataDto.TypeValue.TypeDescriptionType typeDescriptionType:
                Debug.Assert(typeDetails != null, "typeDetails != null");

                return MapFunctionPointerType(
                    typeDescriptionType,
                    (MetadataDto.TypeValue.TypeDetailsFunctionPointer) typeDetails
                );
            case MetadataDto.TypeValue.TypeDescriptionUser typeDescriptionUser:
                DtoNameToDtoEntity.TryGetValue(typeDescriptionUser.Name, out object? userTypeDto);

                MetadataModel.IEntityReference user = userTypeDto switch {
                    MetadataDto.Enumeration enumerationDto => ResolveEnum(enumerationDto),
                    MetadataDto.Struct structDto => ResolveStruct(structDto),
                    MetadataDto.Typedef typedefDto => ResolveTypedef(typedefDto),

                    // FIXME
                    MetadataDto.TypeValue.TypeDescriptionUser =>
                        new MetadataModel.Builtin(
                            typeDescriptionUser.Name,
                            MetadataModel.StorageClass.NotSpecified
                        ).AsEntityReference(),
                    null => throw new InvalidOperationException($"No DTO type found for type [{typeDescriptionUser}]"),
                    _ => throw new ArgumentOutOfRangeException("userTypeDto.DtoEntity", $"{userTypeDto.GetType().FullName}")
                };

                return user;
            default:
                throw new ArgumentOutOfRangeException(nameof(typeDescription), $"{typeDescription.GetType().FullName}");
        }

        return null!;
    }

    private MetadataModel.IEntityReference<MetadataModel.Struct> ResolveStruct(MetadataDto.Struct structDto)
        => ResolveEntity<MetadataDto.Struct, MetadataModel.Struct>(
            structDto.Name,
            structDto,
            dto => {
                List<MetadataModel.IEntityReference<MetadataModel.Delegate>> structDelegates = [];

                MetadataModel.Field MapField(MetadataDto.Field fieldDto) {
                    MetadataModel.IEntityReference field = ResolveTypeValue(fieldDto.Type);

                    MetadataModel.IEntityReference<MetadataModel.Delegate>? fieldDelegateTypeReference =
                        field as MetadataModel.IEntityReference<MetadataModel.Delegate>;

                    if (fieldDelegateTypeReference != null) {
                        structDelegates.Add(fieldDelegateTypeReference);
                    }

                    return new MetadataModel.Field(
                        MetadataDtoToModelMappingUtility.Map(fieldDto.Comments),
                        MetadataDtoToModelMappingUtility.Map(fieldDto.Conditionals).ToEqualityList(),
                        MetadataDtoToModelMappingUtility.Map(fieldDto.SourceLocation),
                        fieldDto.IsInternal,
                        fieldDto.Name,
                        fieldDto.IsAnonymous,
                        false,
                        fieldDto.Width,
                        null,
                        field,
                        fieldDelegateTypeReference
                    );
                }

                return new MetadataModel.Struct(
                    dto.Name,
                    MetadataDtoToModelMappingUtility.Map(dto.Comments),
                    Array.Empty<MetadataModel.Conditional>().ToEqualityList(),
                    MetadataDtoToModelMappingUtility.Map(dto.SourceLocation),
                    dto.IsInternal,
                    dto.Kind switch {
                        MetadataDto.Struct.StructKind.Struct => MetadataModel.Struct.StructKind.Struct,
                        MetadataDto.Struct.StructKind.Union => MetadataModel.Struct.StructKind.Union,
                        _ => throw new ArgumentOutOfRangeException($"Unexpected kind {dto.Kind}")
                    },
                    dto.ByValue,
                    dto.Fields.Select(MapField).ToArray(),
                    structDelegates.ToArray(),
                    []
                );
            });

    private MetadataModel.IEntityReference<MetadataModel.Enum> ResolveEnum(MetadataDto.Enumeration enumerationDto) =>
        ResolveEntity(
            enumerationDto.Name,
            enumerationDto,
            MetadataDtoToModelMappingUtility.Map
        );

    private MetadataModel.IEntityReference<MetadataModel.Typedef> ResolveTypedef(MetadataDto.Typedef typedefDto) =>
        ResolveEntity(
            typedefDto.Name,
            typedefDto,
            MapTypedef
        );

    private MetadataModel.IEntityReference<MetadataModel.Function> ResolveFunction(MetadataDto.Function functionDto) =>
        ResolveEntity<MetadataDto.Function, MetadataModel.Function>(
            functionDto.Name,
            functionDto,
            dto => {
                List<MetadataModel.FunctionArgument> arguments = [];

                foreach (MetadataDto.FunctionArgument argumentDto in dto.Arguments) {
                    MetadataModel.FunctionArgument argument = ResolveFunctionArgument(argumentDto);
                    arguments.Add(argument);
                }

                MetadataModel.IEntityReference returnType = ResolveTypeValue(dto.ReturnType);

                MetadataModel.IEntityReference<MetadataModel.Struct>? ownerClass = null;
                if (dto.OriginalClass != null) {
                    ownerClass = ResolveStruct((MetadataDto.Struct) DtoNameToDtoEntity[dto.OriginalClass]);
                }

                return new MetadataModel.Function(
                    dto.Name,
                    ownerClass,
                    dto.OriginalFullyQualifiedName,
                    dto.IsDefaultArgumentHelper,
                    dto.IsImstrHelper,
                    dto.HasImstrHelper,
                    dto.IsManualHelper,
                    dto.IsUnformattedHelper,
                    arguments.ToEqualityList(),
                    returnType,
                    MetadataDtoToModelMappingUtility.Map(dto.Comments),
                    MetadataDtoToModelMappingUtility.Map(dto.Conditionals).ToEqualityList(),
                    MetadataDtoToModelMappingUtility.Map(dto.SourceLocation),
                    dto.IsInternal
                );
            });

    private MetadataModel.FunctionArgument ResolveFunctionArgument(MetadataDto.FunctionArgument dto) {
        MetadataModel.IEntityReference type =
            dto.IsVarargs ?
                new MetadataModel.Builtin(
                        "varargs",
                        MetadataModel.StorageClass.NotSpecified
                    )
                    .AsEntityReference() :
                ResolveTypeValue(dto.Type);

        MetadataModel.FunctionArgument argument = new(
            dto.Name,
            type,
            dto.IsArray ? dto.ArrayBounds : null,
            dto.IsVarargs,
            dto.IsInstancePointer,
            dto.DefaultValue
        );

        return argument;
    }

    private MetadataModel.IEntityReference<TEntity> ResolveEntity<TDto, TEntity>(
        string dtoName,
        TDto dto,
        Func<TDto, TEntity> resolver
    ) where TEntity : MetadataModel.Entity {
        MetadataModel.IEntityReference<TEntity>? entityReference;
        if (DtoNameToModelEntity.TryGetValue(dtoName, out MetadataModel.IEntityReference? entityReferenceRaw)) {
            entityReference = entityReferenceRaw as MetadataModel.IEntityReference<TEntity>;
            if (entityReference == null)
                throw new InvalidOperationException($"Failed to resolve {nameof(TEntity)} entity for '{dtoName}'");

            return entityReference;
        }

        // Explicitly create an empty reference first, so that when converter is run,
        // it won't endlessly recurse trying to resolve the DTO entity by name
        entityReference = (MetadataModel.IEntityReference<TEntity>) MetadataModel.Entity.CreateEmptyEntityReference<TEntity>();
        bool added = DtoNameToModelEntity.TryAdd(dtoName, entityReference);
        Debug.Assert(added);

        // Convert the DTO to model entity and assign it to the reference
        TEntity userEntity = resolver(dto);
        entityReference.Value = userEntity;

        return entityReference;
    }

    private MetadataModel.IEntityReference<MetadataModel.Delegate> MapFunctionPointerType(
        MetadataDto.TypeValue.TypeDescriptionType typeDescriptionType,
        MetadataDto.TypeValue.TypeDetailsFunctionPointer typeDetailsFunctionPointer
    ) {
        // FIXME: seems like InnerType provides already existing information here?

        //MetadataDto.TypeValue.TypeDescriptionPointer innerPointer =
        //    (MetadataDto.TypeValue.TypeDescriptionPointer) typeDescriptionType.InnerType;
        //Dto.TypeValue.TypeDescriptionFunction innerFunction =
        //    (Dto.TypeValue.TypeDescriptionFunction) innerPointer.InnerType;

        MetadataModel.Delegate modelDelegate = new(
            typeDescriptionType.Name,
            typeDetailsFunctionPointer
                .Arguments
                .Select(ResolveFunctionArgument)
                .ToEqualityList(),
            ResolveTypeValue(typeDetailsFunctionPointer.ReturnType),
            MetadataDtoToModelMappingUtility.Map(typeDetailsFunctionPointer.Comments),
            MetadataDtoToModelMappingUtility.Map(typeDetailsFunctionPointer.Conditionals),
            MetadataDtoToModelMappingUtility.Map(typeDetailsFunctionPointer.SourceLocation),
            typeDetailsFunctionPointer.IsInternal
        );

        return modelDelegate.AsEntityReference();
    }

    private MetadataModel.Typedef MapTypedef(MetadataDto.Typedef dto) =>
        new(
            dto.Name,
            MetadataDtoToModelMappingUtility.Map(dto.Comments),
            MetadataDtoToModelMappingUtility.Map(dto.Conditionals).ToEqualityList(),
            MetadataDtoToModelMappingUtility.Map(dto.SourceLocation),
            dto.IsInternal,
            ResolveTypeValue(dto.Type)
        );
}

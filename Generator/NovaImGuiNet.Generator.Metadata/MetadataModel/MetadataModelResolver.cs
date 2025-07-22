using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.Metadata;

/// <summary>
/// Takes the metadata DTO and generates a model of connected metadata entities.
/// </summary>
public partial class MetadataModelResolver {
    private static readonly ILogger Log = Logging.Create<MetadataModelResolver>();

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

        // This next part is terrible, but it solves the problem of
        // 'how do we have multiple typedefs when we discriminate purely based on name'.
        // In a nutshell, it removes all typedef values from the map
        // (because some typedef might not have came from the top-level typedefs),
        // then resolves typedef again and updates the references.
        // This way, no matter where the typedef was encountered, everything uses the same entity reference.
        foreach (IGrouping<string, MetadataDto.Typedef> typedefsByName in Dto.Typedefs.GroupBy(x => x.Name)) {
            string name = typedefsByName.Key;

            if (DtoNameToModelEntity.TryGetValue(name, out MetadataModel.IEntityReference? entityReference)) {
                if (entityReference is MetadataModel.IEntityReference<MetadataModel.Typedef>) {
                    entityReference.Value = null!;
                }
            }
        }

        foreach (IGrouping<string, MetadataDto.Typedef> typedefsByName in Dto.Typedefs.GroupBy(x => x.Name)) {
            string name = typedefsByName.Key;
            MetadataDto.Typedef[] typedefDtos = typedefsByName.ToArray();

            MetadataModel.Typedef firstTypedefModel = MapTypedef(typedefDtos[0]);

            bool replaced = false;
            if (DtoNameToModelEntity.TryGetValue(name, out MetadataModel.IEntityReference? entityReference)) {
                if (entityReference.Value == null!) {
                    entityReference.Value = firstTypedefModel;
                    replaced = true;
                }
            }

            if (!replaced) {
                allModelEntities.Add(firstTypedefModel.AsEntityReference());
            }

            for (int i = 1; i < typedefDtos.Length; i++) {
                MetadataModel.Typedef typedefModel = MapTypedef(typedefDtos[i]);
                allModelEntities.Add(typedefModel.AsEntityReference());
            }
        }

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
        return CreateOrResolveTypeDescription(
            typeValue.Description,
            typeValue.TypeDetails as MetadataDto.TypeValue.TypeDetailsFunctionPointer
        );
    }

    private MetadataModel.IEntityReference CreateOrResolveTypeDescription(
        MetadataDto.TypeValue.TypeDescription typeDescription,
        MetadataDto.TypeValue.ITypeDetails? typeDetails
    ) {
        switch (typeDescription) {
            case MetadataDto.TypeValue.TypeDescriptionArray typeDescriptionArray:
                return new MetadataModel.Array(
                    CreateOrResolveTypeDescription(typeDescriptionArray.InnerType, null)!,
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
                    CreateOrResolveTypeDescription(typeDescriptionPointer.InnerType, null)!,
                    typeDescriptionPointer.IsNullable
                ).AsEntityReference();
            case MetadataDto.TypeValue.TypeDescriptionType typeDescriptionType:
                Debug.Assert(typeDetails != null);

                MetadataModel.IEntityReference<MetadataModel.Typedef>? existingTypedef =
                    GetEntityReferenceByName<MetadataModel.Typedef>(typeDescriptionType.Name);
                if (existingTypedef != null && existingTypedef.Value != null!)
                    return existingTypedef;

                return MapFunctionPointerType(
                    typeDescriptionType,
                    (MetadataDto.TypeValue.TypeDetailsFunctionPointer) typeDetails
                ).AsEntityReference();
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

                    MetadataModel.IEntityReference fieldType =
                        ResolveTypeValue(fieldDto.Type);

                    MetadataModel.IEntityReference<MetadataModel.Delegate>? fieldDelegateTypeReference =
                        fieldType.ExpandTypedef<MetadataModel.Delegate>();

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
                        fieldDto.DefaultValue,
                        fieldType
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
                    dto.HasPlacementConstructor,
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
                    dto.IsStatic,
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

    private MetadataModel.IEntityReference<TEntity>? GetEntityReferenceByName<TEntity>(string dtoName) where TEntity : MetadataModel.Entity {
        if (DtoNameToModelEntity.TryGetValue(dtoName, out MetadataModel.IEntityReference? entityReferenceRaw))
            return entityReferenceRaw as MetadataModel.IEntityReference<TEntity>;

        return null;
    }

    private MetadataModel.IEntityReference<TEntity> ResolveEntity<TDto, TEntity>(
        string dtoName,
        TDto dto,
        Func<TDto, TEntity> resolver
    ) where TEntity : MetadataModel.Entity {
        MetadataModel.IEntityReference<TEntity>? entityReference = GetEntityReferenceByName<TEntity>(dtoName);
        if (entityReference != null)
            return entityReference;

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

    private MetadataModel.Delegate MapFunctionPointerType(
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

        return modelDelegate;
    }

    private MetadataModel.Typedef MapTypedef(MetadataDto.Typedef dto) {
        MetadataModel.IEntityReference typedefType;

        string name = dto.Name;
        EqualityList<MetadataModel.Conditional> conditionals =
            MetadataDtoToModelMappingUtility.Map(dto.Conditionals).ToEqualityList();
        if (name == dto.Type.Declaration) {
            // HACK: for self-referential typedefs like SDL_Event in cimgui_impl_sdl3
            typedefType =
                new MetadataModel.Builtin(name, MetadataModel.StorageClass.NotSpecified)
                    .AsEntityReference();

            conditionals.Add(new MetadataModel.Conditional("if", "false"));
        } else {
            typedefType = ResolveTypeValue(dto.Type);
        }

        return new MetadataModel.Typedef(
            name,
            MetadataDtoToModelMappingUtility.Map(dto.Comments),
            conditionals,
            MetadataDtoToModelMappingUtility.Map(dto.SourceLocation),
            dto.IsInternal,
            typedefType
        );
    }
}

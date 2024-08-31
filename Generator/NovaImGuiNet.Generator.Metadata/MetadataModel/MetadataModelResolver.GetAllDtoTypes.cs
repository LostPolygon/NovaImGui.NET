namespace NovaImGuiNet.Generator.Metadata;

public partial class MetadataModelResolver {
    private void ResolveAllDtoEntities() {
        foreach (MetadataDto.Enumeration enumeration in Dto.Enums) {
            AddUniqueName(enumeration.Name, enumeration);
        }

        foreach (MetadataDto.Typedef typedef in Dto.Typedefs) {
            AddUniqueName(typedef.Name, typedef);
        }

        foreach (MetadataDto.Struct structure in Dto.Structs) {
            AddUniqueName(structure.Name, structure);
        }

        foreach (MetadataDto.Struct structure in Dto.Structs) {
            foreach (MetadataDto.Field structureField in structure.Fields) {
                VisitTypeValue(structureField.Type);
            }
        }

        foreach (MetadataDto.Function function in Dto.Functions) {
            VisitTypeValue(function.ReturnType);
            foreach (MetadataDto.FunctionArgument argument in function.Arguments) {
                if (argument.IsVarargs)
                    continue;

                VisitTypeValue(argument.Type);
            }
        }

        return;

        void AddUniqueName(string name, object dtoEntity) {
            // It is expected that most names will be encountered a lot of times.
            // This ensures there's only 1 reference to an entity
            DtoNameToDtoEntity.TryAdd(name, dtoEntity);
        }

        void VisitTypeValue(MetadataDto.TypeValue typeValue) {
            if (typeValue.TypeDetails != null) {
                switch (typeValue.TypeDetails) {
                    case MetadataDto.TypeValue.TypeDetailsFunctionPointer typeDetailsFunctionPointer:
                        foreach (MetadataDto.FunctionArgument functionArgument in typeDetailsFunctionPointer.Arguments) {
                            VisitTypeValue(functionArgument.Type);
                        }

                        VisitTypeValue(typeDetailsFunctionPointer.ReturnType);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(typeValue), $"{typeValue.TypeDetails.GetType()}");
                }
            }

            ResolveDtoTypeDescription(typeValue.Description, typeValue.TypeDetails);
        }

        void ResolveDtoTypeDescription(
            MetadataDto.TypeValue.TypeDescription typeDescription,
            MetadataDto.BaseEntity? typeDetails
        ) =>
            VisitDtoTypeDescriptionMap(
                typeDescription,
                typeDetails,
                typeDescriptionUser => AddUniqueName(typeDescriptionUser.Name, typeDescriptionUser),
                typeDescriptionType => {
                    if (typeDetails is MetadataDto.TypeValue.TypeDetailsFunctionPointer) {
                        AddUniqueName(typeDescriptionType.Name, typeDescriptionType);
                    }
                }
            );

        void VisitDtoTypeDescriptionMap(
            MetadataDto.TypeValue.TypeDescription typeDescription,
            MetadataDto.BaseEntity? typeDetails,
            Action<MetadataDto.TypeValue.TypeDescriptionUser> typeDescriptionUserHandler,
            Action<MetadataDto.TypeValue.TypeDescriptionType> typeDescriptionTypeHandler
        ) {
            switch (typeDescription) {
                case MetadataDto.TypeValue.TypeDescriptionArray typeDescriptionArray:
                    ResolveDtoTypeDescription(typeDescriptionArray.InnerType, null);
                    break;
                case MetadataDto.TypeValue.TypeDescriptionBuiltin:
                    break;
                case MetadataDto.TypeValue.TypeDescriptionFunction typeDescriptionFunction:
                    ResolveDtoTypeDescription(typeDescriptionFunction.ReturnType, typeDetails);
                    foreach (MetadataDto.TypeValue.TypeDescription parameter in typeDescriptionFunction.Parameters) {
                        ResolveDtoTypeDescription(parameter, null);
                    }

                    break;
                case MetadataDto.TypeValue.TypeDescriptionPointer typeDescriptionPointer:
                    ResolveDtoTypeDescription(typeDescriptionPointer.InnerType, typeDetails);
                    break;
                case MetadataDto.TypeValue.TypeDescriptionType typeDescriptionType:
                    typeDescriptionTypeHandler(typeDescriptionType);
                    break;
                case MetadataDto.TypeValue.TypeDescriptionUser typeDescriptionUser:
                    typeDescriptionUserHandler(typeDescriptionUser);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeDescription), typeDescription, "Unexpected type");
            }
        }
    }
}

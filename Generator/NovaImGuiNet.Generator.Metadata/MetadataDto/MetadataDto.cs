using System.Text.Json.Serialization;
using static NovaImGuiNet.Generator.Metadata.MetadataDto;

namespace NovaImGuiNet.Generator.Metadata;

// ReSharper disable UnusedMember.Global
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor

public record MetadataDto(
    List<Enumeration> Enums,
    List<Define> Defines,
    List<Typedef> Typedefs,
    List<Struct> Structs,
    List<Function> Functions
) {
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "flavour", IgnoreUnrecognizedTypeDiscriminators = false)]
    [JsonDerivedType(typeof(TypeValue.TypeDetailsFunctionPointer), "function_pointer")]
    public record BaseEntity {
        public Comments Comments { get; init; } = new([], default);
        public Conditional[] Conditionals { get; init; } = [];
        public SourceLocation SourceLocation { get; init; }
        public bool IsInternal { get; init; }
    }

    public record Define : BaseEntity {
        public string Name { get; init; }
        public string? Content { get; init; }
    }

    public record Enumeration : BaseEntity {
        public string Name { get; init; }
        public string OriginalFullyQualifiedName { get; init; }
        public Member[] Elements { get; init; } = [];
        public TypeValue StorageType { get; init; }
        public bool IsFlagsEnum { get; init; }

        public record Member : BaseEntity {
            public string Name { get; init; }
            public long Value { get; init; }
            public string ValueExpression { get; init; }
            public bool IsCount { get; init; }
        }
    }

    public record Typedef : BaseEntity {
        public string Name { get; init; }
        public TypeValue Type { get; init; }
    }

    public record Struct : BaseEntity {
        public string Name { get; init; }
        public string OriginalFullyQualifiedName { get; init; }
        public Field[] Fields { get; init; }
        public StructKind Kind { get; init; }
        public bool ByValue { get; init; }
        public bool ForwardDeclaration { get; init; }
        public bool IsAnonymous { get; init; }

        public enum StructKind {
            Struct,
            Union
        }
    }

    public record Variable(
        string Name,
        TypeValue Type,
        bool IsArray,
        string ArrayBounds,
        int Width
    );

    public record Field : BaseEntity {
        public string Name { get; init; }
        public TypeValue Type { get; init; }
        public bool IsArray { get; init; }
        public string ArrayBounds { get; init; }
        public int? Width { get; init; }

        public bool IsAnonymous { get; init; }
    }

    public record FunctionLike : BaseEntity {
        public FunctionArgument[] Arguments { get; init; }
        public TypeValue ReturnType { get; init; }
    }

    public record FunctionPointer : FunctionLike;

    public record Function : FunctionLike {
        public string Name { get; init; }
        public string? OriginalClass { get; init; }
        public string OriginalFullyQualifiedName { get; init; }
        public bool IsDefaultArgumentHelper { get; init; }
        public bool IsImstrHelper { get; init; }
        public bool HasImstrHelper { get; init; }
        public bool IsManualHelper { get; init; }
        public bool IsUnformattedHelper { get; init; }
    }

    public record Comments(
        string[] Preceding,
        string? Attached
    ) {
        public string[] Preceding { get; set; } = Preceding ?? [];
    }

    public record struct Conditional(
        string Condition,
        string Expression
    );

    public record FunctionArgument {
        public string Name { get; init; }
        public TypeValue Type { get; init; }
        public bool IsArray { get; init; }
        public string ArrayBounds { get; init; }
        public int Width { get; init; }
        public bool IsVarargs { get; init; }
        public bool IsInstancePointer { get; init; }
        public string DefaultValue { get; init; }
    }

    public record SourceLocation(string Filename, int Line);

    public enum StorageClass {
        NotSpecified,
        Const
    }

    public record TypeValue {
        public string Declaration { get; init; }
        public TypeDescription Description { get; init; }
        public BaseEntity? TypeDetails { get; init; }

        public interface IHasStorageClasses {
            StorageClass[]? StorageClasses { get; init; }
        }

        public interface ITypeDetails;

        public record TypeDetailsFunctionPointer : FunctionPointer, ITypeDetails {
            public TypeFlavour Flavour { get; init; }
        }

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "kind", IgnoreUnrecognizedTypeDiscriminators = false)]
        [JsonDerivedType(typeof(TypeDescriptionUser), "User")]
        [JsonDerivedType(typeof(TypeDescriptionArray), "Array")]
        [JsonDerivedType(typeof(TypeDescriptionPointer), "Pointer")]
        [JsonDerivedType(typeof(TypeDescriptionType), "Type")]
        [JsonDerivedType(typeof(TypeDescriptionFunction), "Function")]
        [JsonDerivedType(typeof(TypeDescriptionBuiltin), "Builtin")]
        public abstract record TypeDescription {
            //public TypeDescriptionKind Kind { get; init; }
        }

        public record TypeDescriptionBuiltin : TypeDescription, IHasStorageClasses {
            public string BuiltinType { get; init; }

            public StorageClass[]? StorageClasses { get; init; }
        }

        public record TypeDescriptionUser : TypeDescription, IHasStorageClasses {
            public string Name { get; init; }
            public StorageClass[]? StorageClasses { get; init; }
        }

        public record TypeDescriptionArray : TypeDescription {
            public string Bounds { get; init; }

            public TypeDescription InnerType { get; init; }
        }

        public record TypeDescriptionPointer : TypeDescription, IHasStorageClasses {
            public bool IsNullable { get; init; }
            public TypeDescription InnerType { get; init; }

            public StorageClass[]? StorageClasses { get; init; }
        }

        public record TypeDescriptionType : TypeDescription {
            public string Name { get; init; }
            public TypeDescription InnerType { get; init; }
        }

        public record TypeDescriptionFunction : TypeDescription {
            public TypeDescription[] Parameters { get; init; }
            public TypeDescription ReturnType { get; init; }
        }

        public enum TypeFlavour {
            FunctionPointer
        }

        /*public enum TypeDescriptionKind {
            Builtin,
            User,
            Array,
            Pointer,
            Type,
            Function
        }*/
    }
}

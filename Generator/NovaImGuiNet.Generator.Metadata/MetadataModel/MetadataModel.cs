namespace NovaImGuiNet.Generator.Metadata;

public record MetadataModel(
    IReadOnlyList<MetadataModel.IEntityReference<MetadataModel.Enum>> Enums,
    IReadOnlyList<MetadataModel.IEntityReference<MetadataModel.Struct>> Structs,
    IReadOnlyList<MetadataModel.IEntityReference<MetadataModel.Typedef>> Typedefs,
    IReadOnlyList<MetadataModel.IEntityReference<MetadataModel.Function>> FreeFunctions
) {
    public interface IConvertibleToEntityReference {
        IEntityReference AsEntityReference();
    }

    public abstract record Entity : IConvertibleToEntityReference {
        // Messy hack to have two methods named AsEntityReference with different return types
        protected abstract IEntityReference AsEntityReferenceInternal();

        IEntityReference IConvertibleToEntityReference.AsEntityReference() => AsEntityReferenceInternal();

        // This is smelly, but...
        public static IEntityReference CreateEmptyEntityReference<T>() where T : Entity {
            Type t = typeof(T);
            return t switch {
                not null when t == typeof(Array) => new EntityReference<Array>(),
                not null when t == typeof(Array) => new EntityReference<Array>(),
                not null when t == typeof(Builtin) => new EntityReference<Builtin>(),
                not null when t == typeof(Delegate) => new EntityReference<Delegate>(),
                not null when t == typeof(Enum) => new EntityReference<Enum>(),
                not null when t == typeof(Function) => new EntityReference<Function>(),
                not null when t == typeof(Pointer) => new EntityReference<Pointer>(),
                not null when t == typeof(Struct) => new EntityReference<Struct>(),
                not null when t == typeof(Typedef) => new EntityReference<Typedef>(),
                _ => throw new ArgumentOutOfRangeException(nameof(t), t, "Unexpected type")
            };
        }
    }

    public abstract record Entity<T> : Entity where T : Entity {
        public IEntityReference<T> AsEntityReference() => new EntityReference<T>((this as T)!);

        protected override IEntityReference AsEntityReferenceInternal() => AsEntityReference();
    }

    public interface ITopLevelEntity : IIndependentEntity {
        string Name { get; init; }
    }

    public interface IIndependentEntity {
        Comments Comments { get; init; }
        IReadOnlyList<Conditional> Conditionals { get; init; }
        SourceLocation? SourceLocation { get; init; }
        bool IsInternal { get; init; }
    }

    public abstract record IndependentEntity<T>(
        Comments Comments,
        IReadOnlyList<Conditional> Conditionals,
        SourceLocation? SourceLocation,
        bool IsInternal
    ) : Entity<T>, IIndependentEntity where T : Entity;

    public abstract record TopLevelEntity<T>(
        Comments Comments,
        IReadOnlyList<Conditional> Conditionals,
        SourceLocation? SourceLocation,
        bool IsInternal,
        string Name
    ) : IndependentEntity<T>(Comments, Conditionals, SourceLocation, IsInternal),
        ITopLevelEntity
        where T : Entity {
        public override string ToString() {
            return $"{Name}";
        }
    }

    public record Builtin(
        string Type,
        StorageClass StorageClass
    ) : Entity<Builtin>;

    public record Array(
        IEntityReference ElementType,
        string? ArrayBounds,
        long? ArrayBoundsNumeric
    ) : Entity<Array>;

    public record Pointer(
        IEntityReference ValueType,
        bool IsNullable
    ) : Entity<Pointer>;

    public record Enum(
        string Name,
        Comments Comments,
        IReadOnlyList<Conditional> Conditionals,
        SourceLocation? SourceLocation,
        bool IsInternal,
        IReadOnlyList<Enum.Member> Members,
        bool IsFlagsEnum
    ) : TopLevelEntity<Enum>(Comments, Conditionals, SourceLocation, IsInternal, Name) {
        public override string ToString() {
            return $"{{ {nameof(Enum)} ({base.ToString()}) }}";
        }

        public record Member(
            string Name,
            string? Value,
            long? ValueNumeric,
            Comments Comments,
            IReadOnlyList<Conditional> Conditionals,
            SourceLocation? SourceLocation,
            bool IsInternal
        ) : IndependentEntity<Member>(Comments, Conditionals, SourceLocation, IsInternal);
    }

    public record Struct(
        string Name,
        Comments Comments,
        IReadOnlyList<Conditional> Conditionals,
        SourceLocation? SourceLocation,
        bool IsInternal,
        Struct.StructKind Kind,
        bool ByValue,
        IReadOnlyList<Field> Fields,
        IReadOnlyList<IEntityReference<Delegate>> Delegates,
        IReadOnlyList<IEntityReference<Function>> Functions
    ) : TopLevelEntity<Struct>(Comments, Conditionals, SourceLocation, IsInternal, Name) {
        public override string ToString() {
            return $"{{ {nameof(Struct)} ({base.ToString()}) }}";
        }

        public enum StructKind {
            StaticClass,
            Struct,
            Union
        }
    }

    public record Typedef(
        string Name,
        Comments Comments,
        IReadOnlyList<Conditional> Conditionals,
        SourceLocation? SourceLocation,
        bool IsInternal,
        IEntityReference Type
    ) : TopLevelEntity<Typedef>(Comments, Conditionals, SourceLocation, IsInternal, Name) {
        public override string ToString() {
            return $"{{ {nameof(Typedef)} {{ {nameof(Type)} = {Type}, FullName = {base.ToString()} }} }}";
        }
    }

    public record Field(
        Comments Comments,
        IReadOnlyList<Conditional> Conditionals,
        SourceLocation? SourceLocation,
        bool IsInternal,
        string Name,
        bool IsAnonymous,
        bool IsConstant,
        int? Width,
        string? Value,
        IEntityReference Type
    ) : IndependentEntity<Field>(Comments, Conditionals, SourceLocation, IsInternal);

    public enum StorageClass {
        NotSpecified,
        Const
    }

    public record FunctionArgument(
        string Name,
        IEntityReference Type,
        string? ArrayBounds,
        bool IsVarargs,
        bool IsInstancePointer,
        string? DefaultValue
    );

    public interface IFunctionLike {
        IReadOnlyList<FunctionArgument> Arguments { get; init; }
        IEntityReference ReturnType { get; init; }
    }

    public abstract record FunctionLike<T>(
        IReadOnlyList<FunctionArgument> Arguments,
        IEntityReference ReturnType,
        Comments Comments,
        IReadOnlyList<Conditional> Conditionals,
        SourceLocation? SourceLocation,
        bool IsInternal
    ) : IndependentEntity<T>(Comments, Conditionals, SourceLocation, IsInternal), IFunctionLike where T : Entity {
        protected string ToStringBase()
            => $"{ReturnType.Value} ({String.Join(", ", Arguments.Select(x => $"{x.Type.Value} {x.Name}"))})";
    }

    public record Delegate(
        string Name,
        IReadOnlyList<FunctionArgument> Arguments,
        IEntityReference ReturnType,
        Comments Comments,
        IReadOnlyList<Conditional> Conditionals,
        SourceLocation? SourceLocation,
        bool IsInternal
    ) : FunctionLike<Delegate>(Arguments, ReturnType, Comments, Conditionals, SourceLocation, IsInternal) {
        public override string ToString() {
            return $"{{ {nameof(Delegate)} {{ {ToStringBase()} }} }}";
        }
    }

    public record Function(
        string Name,
        IEntityReference<Struct>? OriginalClass,
        string OriginalFullyQualifiedName,
        bool IsDefaultArgumentHelper,
        bool IsImstrHelper,
        bool HasImstrHelper,
        bool IsManualHelper,
        bool IsUnformattedHelper,
        IReadOnlyList<FunctionArgument> Arguments,
        IEntityReference ReturnType,
        Comments Comments,
        IReadOnlyList<Conditional> Conditionals,
        SourceLocation? SourceLocation,
        bool IsInternal
    ) : FunctionLike<Function>(Arguments, ReturnType, Comments, Conditionals, SourceLocation, IsInternal) {
        public override string ToString() {
            return
                $"{{ {nameof(Function)} {{ " +
                $"[{(OriginalClass != null ? OriginalClass.Value.ToString() : "free")}]" +
                $" {Name} - {ToStringBase()}  }} }}";
        }
    }

    public readonly record struct Comments(
        IReadOnlyList<string> Preceding,
        IReadOnlyList<string> Attached
    );

    public record struct Conditional(string Condition, string Expression);

    public record SourceLocation(string Filename, int Line);

    public interface IEntityReference {
        Entity Value { get; set; }
    }

    public interface IEntityReference<T> : IEntityReference where T : Entity {
        new T Value { get; set; }
    }

    public record EntityReference<T> : IEntityReference<T> where T : Entity {
        public T Value { get; set; }

        public EntityReference() : this((T) null!) {
        }

        public EntityReference(T value) {
            Value = value;
        }

        Entity IEntityReference.Value {
            get => Value;
            set => Value = (T) value;
        }

        public override string ToString() {
            if (Value == null!)
                return "Value = null";

            return Value.ToString();
        }
    }
}

using Microsoft.CodeAnalysis.CSharp.Syntax;
using OneOf;
using NovaImGuiNet.Generator.CSharp;

namespace NovaImGuiNet.Generator.CSharp;

public record BindingsModel : BindingsModel.IEntity {
    public BindingsModel() {
    }

    public required List<IEntityReference<Enum>> Enums { get; set; }
    public required List<IEntityReference<Struct>> Structs { get; set; }
    public required List<IEntityReference<Typedef>> Typedefs { get; set; }
    public required List<IEntityReference<Function>> FreeFunctions { get; set; }

    public IReadOnlyList<IEntityReference<Function>> GetAllFunctions() {
        return [
            ..FreeFunctions,
            ..Structs.SelectMany(s => s.Value.Children.OfType<IEntityReference<Function>>())
        ];
    }

    public IEnumerable<FunctionList> GetAllFunctionLists() {
        return [
            new FunctionList(null, FreeFunctions, FreeFunctions.ToArray()),
            ..Structs.Select(s => new FunctionList(
                s,
                s.Value.Children,
                s.Value.Children.OfType<IEntityReference<Function>>().ToArray()
            ))
        ];
    }

    public record struct FunctionList(
        IEntityReference<Struct>? Structure,
        OneOf<List<IEntityReference<Function>>, ChildrenEntityList> OwningList,
        IReadOnlyList<IEntityReference<Function>> FunctionsOnlyList
    );

    public IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes) {
        foreach (var entity in Enums) {
            foreach (IEntityReference child in entity.Value.GetTypes(visitedNodes)) {
                yield return child;
            }
        }

        foreach (var entity in Structs) {
            foreach (IEntityReference child in entity.Value.GetTypes(visitedNodes)) {
                yield return child;
            }
        }

        foreach (var entity in Typedefs) {
            foreach (IEntityReference child in entity.Value.GetTypes(visitedNodes)) {
                yield return child;
            }
        }

        foreach (var entity in FreeFunctions) {
            foreach (IEntityReference child in entity.Value.GetTypes(visitedNodes)) {
                yield return child;
            }
        }
    }

    public interface IConvertibleToEntityReference {
        IEntityReference AsEntityReference();
    }

    public interface IEntity {
        IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes);
    }

    public interface ITopLevelEntity : IIndependentEntity {
        string Name { get; set; }
        string Namespace { get; set; }
    }

    public interface IIndependentEntity : IEntity {
        Comments Comments { get; set; }
        IReadOnlyList<Conditional> Conditionals { get; set; }
        bool IsInternal { get; set; }
        bool IsDisabled { get; set; }
        bool SkipEmit { get; set; }
    }

    public abstract record SyntaxEntity<T> : IIndependentEntity {
        public T Syntax { get; set; }
        public Comments Comments { get; set; } = new();

        public IReadOnlyList<Conditional> Conditionals { get; set; } = [];
        public bool IsInternal { get; set; }
        public bool IsDisabled { get; set; }
        public bool SkipEmit { get; set; }

        protected SyntaxEntity(T syntax) {
            Syntax = syntax;
        }

        protected SyntaxEntity(
            T syntax,
            IIndependentEntity other
        ) : this(syntax, other.Comments, other.Conditionals, other.IsInternal, other.IsDisabled, other.SkipEmit) {
        }

        protected SyntaxEntity(
            T syntax,
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal,
            bool isDisabled,
            bool skipEmit
        ) {
            Syntax = syntax;
            Comments = comments;
            Conditionals = conditionals;
            IsInternal = isInternal;
            IsDisabled = isDisabled;
            SkipEmit = skipEmit;
        }

        public IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes) {
            yield break;
        }
    }

    public record MemberDeclarationSyntaxEntity : SyntaxEntity<MemberDeclarationSyntax> {
        public MemberDeclarationSyntaxEntity(MemberDeclarationSyntax syntax) : base(syntax) {
        }

        public MemberDeclarationSyntaxEntity(
            MemberDeclarationSyntax syntax,
            IIndependentEntity other
        ) : base(syntax, other) {
        }

        public MemberDeclarationSyntaxEntity(
            MemberDeclarationSyntax syntax,
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal,
            bool isDisabled,
            bool skipEmit
        ) : base(syntax, comments, conditionals, isInternal, isDisabled, skipEmit) {
        }
    }

    public abstract record Entity : IConvertibleToEntityReference, IEntity {
        public virtual IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes) {
            yield break;
        }

        // Messy hack to have two methods named AsEntityReference with different return types
        protected abstract IEntityReference AsAnyEntityReferenceInternal();

        IEntityReference IConvertibleToEntityReference.AsEntityReference() => AsAnyEntityReferenceInternal();

        // This is smelly, but...
        public static IEntityReference CreateEmptyEntityReference(Type t) {
            return t switch {
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

        protected override IEntityReference AsAnyEntityReferenceInternal() => AsEntityReference();
    }

    public abstract record IndependentEntity<T> : Entity<T>, IIndependentEntity where T : Entity {
        public Comments Comments { get; set; } = new();
        public IReadOnlyList<Conditional> Conditionals { get; set; } = [];
        public bool IsInternal { get; set; }
        public bool IsDisabled { get; set; }
        public bool SkipEmit { get; set; }

        protected IndependentEntity() {
        }

        protected IndependentEntity(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal
        ) {
            Comments = comments;
            Conditionals = conditionals;
            IsInternal = isInternal;
        }
    }

    public abstract record TopLevelEntity<T> : IndependentEntity<T>, ITopLevelEntity where T : Entity {
        public required string Name { get; set; }
        public required string Namespace { get; set; }

        protected TopLevelEntity() {
        }

        protected TopLevelEntity(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal
        ) : base(comments, conditionals, isInternal) {
        }

        public override string ToString() {
            return $"{Namespace}.{Name}";
        }
    }

    public record Builtin(
        string BuiltinName,
        Type? ManagedType,
        StorageClass StorageClass
    ) : Entity<Builtin>;

    public record Array(
        IEntityReference ElementType,
        string? ArrayBounds,
        long? ArrayBoundsNumeric
    ) : Entity<Array> {
        public override IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes) {
            if (visitedNodes.Add(ElementType)) {
                yield return ElementType;

                foreach (IEntityReference elementTypeChild in ElementType.Value.GetTypes(visitedNodes)) {
                    yield return elementTypeChild;
                }
            }
        }
    }

    public record Pointer(
        IEntityReference ValueType,
        bool IsNullable
    ) : Entity<Pointer> {
        public override IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes) {
            if (visitedNodes.Add(ValueType)) {
                yield return ValueType;
                foreach (IEntityReference valueTypeChild in ValueType.Value.GetTypes(visitedNodes)) {
                    yield return valueTypeChild;
                }
            }
        }
    }

    public record Enum : TopLevelEntity<Enum> {
        public required List<Member> Members { get; set; }
        public bool IsFlagsEnum { get; set; }

        public Enum() {
        }

        public Enum(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal,
            bool isFlagsEnum
        ) : base(comments, conditionals, isInternal) {
            IsFlagsEnum = isFlagsEnum;
        }

        public override string ToString() {
            return $"{{ {nameof(Enum)} ({base.ToString()}) }}";
        }

        public record Member : IndependentEntity<Member> {
            public required string Name { get; set; }
            public required string? Value { get; set; }
            public required long? ValueNumeric { get; set; }

            public Member() {
            }

            public Member(
                Comments comments,
                IReadOnlyList<Conditional> conditionals,
                bool isInternal
            ) : base(comments, conditionals, isInternal) {
            }
        }
    }

    public record Struct : TopLevelEntity<Struct> {
        private static readonly IReadOnlyList<Type> AllowedChildrenTypes = [
            typeof(Field),
            typeof(IEntityReference<Delegate>),
            typeof(SyntaxEntity<MemberDeclarationSyntax>),
        ];

        public required StructKind Kind { get; init; }
        public bool ByValue { get; init; }
        public IReadOnlyList<IEntityReference> GenericArguments { get; init; } = [];

        public ChildrenEntityList Children { get; init; } = new(AllowedChildrenTypes);

        public Struct() {
        }

        public Struct(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal,
            bool byValue,
            IReadOnlyList<IEntityReference> genericArguments,
            IReadOnlyList<IEntity> children
        ) : base(comments, conditionals, isInternal) {
            ByValue = byValue;
            GenericArguments = genericArguments;
            Children.AddRange(children);
        }

        public override IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes) {
            foreach (IEntityReference genericArgument in GenericArguments) {
                if (visitedNodes.Add(genericArgument)) {
                    foreach (IEntityReference genericArgumentChild in genericArgument.Value.GetTypes(visitedNodes)) {
                        yield return genericArgumentChild;
                    }
                }
            }

            foreach (IEntityReference child in Children.OfType<IEntityReference>()) {
                if (visitedNodes.Add(child)) {
                    foreach (IEntityReference childChild in child.Value.GetTypes(visitedNodes)) {
                        yield return childChild;
                    }
                }
            }
        }

        public override string ToString() {
            return $"{{ {nameof(Struct)} ({base.ToString()}) }}";
        }

        public enum StructKind {
            Struct,
            StaticClass,
            Union
        }
    }

    public record Typedef : TopLevelEntity<Typedef> {
        public required IEntityReference Type { get; init; }

        public Typedef() {
        }

        public Typedef(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal
        ) : base(comments, conditionals, isInternal) {
        }

        public override IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes) {
            if (visitedNodes.Add(Type)) {
                yield return Type;

                foreach (IEntityReference typeChild in Type.Value.GetTypes(visitedNodes)) {
                    yield return typeChild;
                }
            }
        }

        public override string ToString() {
            return $"{{ {nameof(Typedef)} {{ {nameof(Type)} = {Type}, FullName = {base.ToString()} }} }}";
        }
    }

    public record Field : IndependentEntity<Field> {
        public required string Name { get; set; }
        public required string? Value { get; set; }
        public required IEntityReference Type { get; set; }
        public bool IsConstant { get; set; }
        public int? Width { get; set; }
        public IEntityReference? DelegateType { get; set; }

        public Field() {
        }

        public Field(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal,
            bool isConstant,
            int? width,
            IEntityReference? delegateType
        ) : base(comments, conditionals, isInternal) {
            IsConstant = isConstant;
            Width = width;
            DelegateType = delegateType;
        }
    }

    public enum StorageClass {
        NotSpecified,
        Const
    }

    public record FunctionArgument {
        public required string Name { get; set; }
        public required IEntityReference Type { get; set; }
        public required string? ArrayBounds { get; set; }
        public required bool IsVarargs { get; set; }
        public required bool IsInstancePointer { get; set; }
        public required string? DefaultValue { get; set; }
    }

    public interface IFunctionLike : IIndependentEntity {
        EqualityList<FunctionArgument> Arguments { get; set; }
        IEntityReference ReturnType { get; set; }
    }

    public abstract record FunctionLike<T> : IndependentEntity<T>, IFunctionLike where T : Entity {
        public required EqualityList<FunctionArgument> Arguments { get; set; }
        public required IEntityReference ReturnType { get; set; }

        protected FunctionLike() {
        }

        protected FunctionLike(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal
        ) : base(comments, conditionals, isInternal) {
        }

        public IEnumerable<IEntityReference> GetAllTypes(HashSet<IEntityReference> visitedNodes) {
            if (visitedNodes.Add(ReturnType)) {
                yield return ReturnType;

                foreach (FunctionArgument argument in Arguments) {
                    foreach (IEntityReference argumentChild in argument.Type.Value.GetTypes(visitedNodes)) {
                        yield return argumentChild;
                    }
                }
            }
        }

        protected string ToStringBase() => CSharpCodeGenerationUtility.FormatType(this);

        public override IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes) {
            if (visitedNodes.Add(ReturnType)) {
                yield return ReturnType;

                foreach (FunctionArgument argument in Arguments) {
                    foreach (IEntityReference argumentChild in argument.Type.Value.GetTypes(visitedNodes)) {
                        yield return argumentChild;
                    }
                }
            }
        }
    }

    public record Delegate : FunctionLike<Delegate> {
        public required string Name { get; set; }

        public Delegate() {
        }

        public Delegate(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal
        ) : base(comments, conditionals, isInternal) {
        }

        public override string ToString() {
            return $"{{ {nameof(Delegate)} {{ {ToStringBase()} }} }}";
        }
    }

    public record Function : FunctionLike<Function> {
        public required string Name { get; set; }
        public IEntityReference<Struct>? OriginalClass { get; set; }
        public string? OriginalFullyQualifiedName { get; set; }
        public bool IsDefaultArgumentHelper { get; set; }
        public bool IsImstrHelper { get; set; }
        public bool HasImstrHelper { get; set; }
        public bool IsManualHelper { get; set; }
        public bool IsUnformattedHelper { get; set; }

        public Function() {
        }

        public Function(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal,
            IEntityReference<Struct>? originalClass,
            string originalFullyQualifiedName,
            bool isDefaultArgumentHelper,
            bool isImstrHelper,
            bool hasImstrHelper,
            bool isManualHelper,
            bool isUnformattedHelper
        ) : base(comments, conditionals, isInternal) {
            OriginalClass = originalClass;
            OriginalFullyQualifiedName = originalFullyQualifiedName;
            IsDefaultArgumentHelper = isDefaultArgumentHelper;
            IsImstrHelper = isImstrHelper;
            HasImstrHelper = hasImstrHelper;
            IsManualHelper = isManualHelper;
            IsUnformattedHelper = isUnformattedHelper;
        }

        public override IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes) {
            if (OriginalClass != null && visitedNodes.Add(OriginalClass)) {
                yield return OriginalClass;
            }

            foreach (IEntityReference child in base.GetTypes(visitedNodes)) {
                yield return child;
            }
        }

        public override string ToString() {
            return
                $"{{ {nameof(Function)} {{ " +
                $"[{(OriginalClass != null ? OriginalClass.Value.Name : "free")}]" +
                $" {ToStringBase()}  }} }}";
        }
    }

    public record Comments(
        IReadOnlyList<string> Preceding,
        IReadOnlyList<string> Attached,
        IReadOnlyList<string> Notes
    ) {
        public Comments() : this([], [], []) {
        }

        public Comments WithAddedNotes(params string[] notes) {
            return this with {
                Notes = new EqualityList<string>([..Notes, ..notes])
            };
        }
    }

    public record struct Conditional(
        string Condition,
        string Expression
    );

    public interface IEntityReference {
        Entity Value { get; set; }
    }

    public interface IEntityReference<T> : IEntityReference, IEntity {
        new T Value { get; set; }
    }

    public record EntityReference<T> : IEntityReference<T> where T : Entity {
        public T Value { get; set; }

        public EntityReference() : this((T) null!) {
        }

        public EntityReference(T value) {
            Value = value;
        }

        public IEnumerable<IEntityReference> GetTypes(HashSet<IEntityReference> visitedNodes) {
            foreach (IEntityReference child in Value.GetTypes(visitedNodes)) {
                yield return child;
            }
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

    public void Deconstruct(
        out List<IEntityReference<Enum>> Enums,
        out List<IEntityReference<Struct>> Structs,
        out List<IEntityReference<Typedef>> Typedefs,
        out List<IEntityReference<Function>> FreeFunctions
    ) {
        Enums = this.Enums;
        Structs = this.Structs;
        Typedefs = this.Typedefs;
        FreeFunctions = this.FreeFunctions;
    }
}

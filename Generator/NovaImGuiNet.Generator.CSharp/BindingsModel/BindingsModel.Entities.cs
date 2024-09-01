using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NovaImGuiNet.Generator.CSharp;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable PropertyCanBeMadeInitOnly.Global
public partial class BindingsModel {
    public interface IConvertibleToEntityReference {
        IEntityReference AsEntityReference();
    }

    public interface IEntity {
        IEnumerable<IEntityReference> GetReferencedTypes(HashSet<IEntityReference> visitedNodes);
    }

    public interface IIndependentEntity : IEntity {
        Comments Comments { get; set; }
        IReadOnlyList<Conditional> Conditionals { get; set; }
        bool IsInternal { get; set; }
        bool IsDisabled { get; set; }
        bool SkipEmit { get; set; }
    }

    public interface ITopLevelEntity : IIndependentEntity {
        string Name { get; set; }
        string Namespace { get; set; }
    }

    public abstract record Entity : IConvertibleToEntityReference, IEntity {
        public virtual IEnumerable<IEntityReference> GetReferencedTypes(HashSet<IEntityReference> visitedNodes) {
            yield break;
        }

        // Messy hack to have two methods named AsEntityReference with different return types
        public IEntityReference AsEntityReference() => new EntityReference(this);
    }

    public abstract record IndependentEntity : Entity, IIndependentEntity {
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

        protected override bool PrintMembers(StringBuilder builder) {
            return false;
        }
    }

    public abstract record TopLevelEntity : IndependentEntity, ITopLevelEntity {
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
    ) : Entity;

    public record Array(
        IEntityReference ElementType,
        string? ArrayBounds,
        long? ArrayBoundsNumeric
    ) : Entity {
        public override IEnumerable<IEntityReference> GetReferencedTypes(HashSet<IEntityReference> visitedNodes) {
            if (visitedNodes.Add(ElementType)) {
                yield return ElementType;

                foreach (IEntityReference elementTypeChild in ElementType.Value.GetReferencedTypes(visitedNodes)) {
                    yield return elementTypeChild;
                }
            }
        }
    }

    public record Pointer(
        IEntityReference ValueType,
        bool IsNullable
    ) : Entity {
        public override IEnumerable<IEntityReference> GetReferencedTypes(HashSet<IEntityReference> visitedNodes) {
            if (visitedNodes.Add(ValueType)) {
                yield return ValueType;
                foreach (IEntityReference valueTypeChild in ValueType.Value.GetReferencedTypes(visitedNodes)) {
                    yield return valueTypeChild;
                }
            }
        }
    }

    public record Enum : TopLevelEntity {
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

        public record Member : IndependentEntity {
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

    public record Struct : TopLevelEntity {
        private static readonly IReadOnlyList<Type> AllowedChildrenTypes = [
            typeof(Field),
            typeof(IEntityReference),
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

        public override IEnumerable<IEntityReference> GetReferencedTypes(HashSet<IEntityReference> visitedNodes) {
            foreach (IEntityReference genericArgument in GenericArguments) {
                if (visitedNodes.Add(genericArgument)) {
                    foreach (IEntityReference genericArgumentChild in genericArgument.Value.GetReferencedTypes(visitedNodes)) {
                        yield return genericArgumentChild;
                    }
                }
            }

            foreach (IEntityReference child in Children.OfType<IEntityReference>()) {
                if (visitedNodes.Add(child)) {
                    foreach (IEntityReference childChild in child.Value.GetReferencedTypes(visitedNodes)) {
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

    public record Typedef : TopLevelEntity {
        public required IEntityReference Type { get; set; }

        public Typedef() {
        }

        public Typedef(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal
        ) : base(comments, conditionals, isInternal) {
        }

        public override IEnumerable<IEntityReference> GetReferencedTypes(HashSet<IEntityReference> visitedNodes) {
            if (visitedNodes.Add(Type)) {
                yield return Type;

                foreach (IEntityReference typeChild in Type.Value.GetReferencedTypes(visitedNodes)) {
                    yield return typeChild;
                }
            }
        }

        public override string ToString() {
            return $"{{ {nameof(Typedef)} {{ {nameof(Type)} = {Type}, FullName = {base.ToString()} }} }}";
        }
    }

    public record Field : IndependentEntity {
        public required string Name { get; set; }
        public required string? Value { get; set; }
        public required IEntityReference Type { get; set; }
        public bool IsConstant { get; set; }
        public int? BitWidth { get; set; }

        public Field() {
        }

        public Field(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal,
            bool isConstant,
            int? bitWidth
        ) : base(comments, conditionals, isInternal) {
            IsConstant = isConstant;
            BitWidth = bitWidth;
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
        public required string? DeclarationDefaultValue { get; set; }
        public string? DefaultValue { get; set; }
    }

    public interface IFunctionLike : IIndependentEntity {
        List<FunctionArgument> Arguments { get; set; }
        IEntityReference ReturnType { get; set; }
    }

    public abstract record FunctionLike : IndependentEntity, IFunctionLike {
        public required List<FunctionArgument> Arguments { get; set; }
        public required IEntityReference ReturnType { get; set; }

        protected FunctionLike() {
        }

        protected FunctionLike(
            Comments comments,
            IReadOnlyList<Conditional> conditionals,
            bool isInternal
        ) : base(comments, conditionals, isInternal) {
        }

        public override IEnumerable<IEntityReference> GetReferencedTypes(HashSet<IEntityReference> visitedNodes) {
            if (visitedNodes.Add(ReturnType)) {
                yield return ReturnType;

                foreach (FunctionArgument argument in Arguments) {
                    foreach (IEntityReference argumentChild in argument.Type.Value.GetReferencedTypes(visitedNodes)) {
                        yield return argumentChild;
                    }
                }
            }
        }

        protected string ToStringBase() => CSharpCodeGenerationUtility.FormatType(this);
    }

    public record Delegate : FunctionLike {
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

    public record Function : FunctionLike {
        public required string Name { get; set; }
        public EntityReferenceWrapper<Struct>? OriginalClass { get; set; }
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
            EntityReferenceWrapper<Struct>? originalClass,
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

        public override IEnumerable<IEntityReference> GetReferencedTypes(HashSet<IEntityReference> visitedNodes) {
            if (OriginalClass != null && visitedNodes.Add(OriginalClass.Reference)) {
                yield return OriginalClass.Reference;
            }

            foreach (IEntityReference child in base.GetReferencedTypes(visitedNodes)) {
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
                Notes = [..Notes, ..notes]
            };
        }
    }

    public record struct Conditional(string Condition, string Expression) {
        public Conditional(string expression) : this("if", expression) {
        }
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

        public IEnumerable<IEntityReference> GetReferencedTypes(HashSet<IEntityReference> visitedNodes) {
            yield break;
        }

        protected virtual bool PrintMembers(StringBuilder builder) {
            builder.Append($"{nameof(Syntax)} = {Syntax}");
            return true;
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

    public record EntityReferenceWrapper<T>(IEntityReference Reference) where T : Entity {
        public T Value => (T) Reference.Value;
    }

    public interface IEntityReference {
        Entity Value { get; set; }
    }

    private record EntityReference(Entity Value) : IEntityReference, IEntity {
        public Entity Value { get; set; } = Value;

        public EntityReference() : this((Entity) null!) {
        }

        public IEnumerable<IEntityReference> GetReferencedTypes(HashSet<IEntityReference> visitedNodes) {
            foreach (IEntityReference child in Value.GetReferencedTypes(visitedNodes)) {
                yield return child;
            }
        }

        public override string ToString() {
            if (Value == null!)
                return "Value = null";

            return Value.ToString();
        }
    }
}

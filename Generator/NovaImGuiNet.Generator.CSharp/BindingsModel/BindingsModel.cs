namespace NovaImGuiNet.Generator.CSharp;

public partial class BindingsModel {
    public List<IEntityReference> Children { get; } = [];

    public IReadOnlyList<EntityReferenceWrapper<Enum>> Enums => GetChildren<Enum>();
    public IReadOnlyList<EntityReferenceWrapper<Struct>> Structs => GetChildren<Struct>();
    public IReadOnlyList<EntityReferenceWrapper<Typedef>> Typedefs => GetChildren<Typedef>();
    public IReadOnlyList<EntityReferenceWrapper<Function>> FreeFunctions => GetChildren<Function>();

    public List<CompilationUnitWithFileInformation> ExtraCompilationUnits { get; } = [];

    public BindingsModel() {
    }

    public BindingsModel(List<IEntityReference> children) {
        Children = children;
    }

    public IReadOnlyList<EntityReferenceWrapper<Function>> GetAllFunctions() {
        IEnumerable<EntityReferenceWrapper<Function>> structFunctions =
            Structs
                .SelectMany(structure =>
                    structure.Value.Children
                        .OfType<IEntityReference>()
                        .Where(x => x.Value is Function)
                )
                .Select(x => new EntityReferenceWrapper<Function>(x));

        return [
            ..FreeFunctions,
            ..structFunctions
        ];
    }

    public IReadOnlyList<EntityReferenceWrapper<Delegate>> GetAllDelegates() {
        IEnumerable<EntityReferenceWrapper<Delegate>> structDelegates =
            Structs
                .SelectMany(structure => {
                    List<IEntityReference> childDelegates = [];

                    foreach (IEntityReference child in structure.Value.Children.OfType<IEntityReference>()) {
                        if (child.Value is Delegate) {
                            childDelegates.Add(child);
                        }
                    }

                    foreach (Field field in structure.Value.Children.OfType<Field>()) {
                        IEntityReference? fieldDelegate = field.Type.ExpandAllTypedefsAndPointers<Delegate>();
                        if (fieldDelegate != null) {
                            childDelegates.Add(fieldDelegate);
                        }
                    }

                    return childDelegates;
                })
                .Select(x => new EntityReferenceWrapper<Delegate>(x));

        return new List<EntityReferenceWrapper<Delegate>>([
            ..Typedefs
                .Where(typedef => typedef.Value.Type.Value is Delegate)
                .Select(typedef => new EntityReferenceWrapper<Delegate>(typedef.Value.Type)),
            ..structDelegates
        ]).ToList();
    }

    private List<EntityReferenceWrapper<T>> GetChildren<T>() where T : Entity =>
        Children
            .Where(x => x.Value is T)
            .Select(x => new EntityReferenceWrapper<T>(x))
            .ToList();

    public static IEntityReference CreateEmptyEntityReference() => new EntityReference();
}

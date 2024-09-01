using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

public partial class BindingsModel {
    public List<IEntityReference> Children { get; }

    public IReadOnlyList<EntityReferenceWrapper<Enum>> Enums => GetChildren<Enum>();
    public IReadOnlyList<EntityReferenceWrapper<Struct>> Structs => GetChildren<Struct>();
    public IReadOnlyList<EntityReferenceWrapper<Typedef>> Typedefs => GetChildren<Typedef>();
    public IReadOnlyList<EntityReferenceWrapper<Function>> FreeFunctions => GetChildren<Function>();

    public BindingsModel() {
    }

    public BindingsModel(List<IEntityReference> children) {
        Children = children;
    }

    public IReadOnlyList<EntityReferenceWrapper<Function>> GetAllFunctions() {
        IEnumerable<EntityReferenceWrapper<Function>> structFunctions =
            Structs
                .SelectMany(s =>
                    s.Value.Children
                        .OfType<IEntityReference>()
                        .Where(x => x.Value is Function)
                )
                .Select(x => new EntityReferenceWrapper<Function>(x));

        return [
            ..FreeFunctions,
            ..structFunctions
        ];
    }

    private List<EntityReferenceWrapper<T>> GetChildren<T>() where T : Entity {
        return Children
            .Where(x => x.Value is T)
            .Select(x => new EntityReferenceWrapper<T>(x))
            .ToList();
    }

    public static IEntityReference CreateEmptyEntityReference() => new EntityReference();
}

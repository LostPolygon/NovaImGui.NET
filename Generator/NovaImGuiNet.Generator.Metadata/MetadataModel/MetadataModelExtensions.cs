namespace NovaImGuiNet.Generator.Metadata;

using static MetadataModel;

public static class MetadataModelExtensions {
    public static IEntityReference AsEntityReference(this Entity entity) =>
        ((IConvertibleToEntityReference) entity).AsEntityReference();

    public static IEntityReference<T>? ExpandTypedef<T>(this IEntityReference? entityReference) where T : Entity {
        if (entityReference == null)
            return null;

        if (entityReference is IEntityReference<T> found)
            return found;

        switch (entityReference) {
            case IEntityReference<Typedef> typedef:
                return ExpandTypedef<T>(typedef.Value.Type);
        }

        return null;
    }
}

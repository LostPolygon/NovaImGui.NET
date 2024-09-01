namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;

public static class BindingsModelExtensions {
    public static IEntityReference AsEntityReference(this Entity entity) =>
        ((IConvertibleToEntityReference) entity).AsEntityReference();

    public static IEntityReference<T>? ExpandWrappers<T>(this IEntityReference? entityReference) where T : Entity {
        if (entityReference == null)
            return null;

        if (entityReference is IEntityReference<T> found)
            return found;

        switch (entityReference) {
            case IEntityReference<Pointer> pointer:
                return ExpandWrappers<T>(pointer.Value.ValueType);
            case IEntityReference<Typedef> typedef:
                return ExpandWrappers<T>(typedef.Value.Type);
        }

        return null;
    }
}

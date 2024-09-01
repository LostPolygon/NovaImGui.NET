namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;

public static class BindingsModelExtensions {
    public static IEntityReference? ExpandWrappers<T>(this IEntityReference? entityReference) where T : Entity {
        if (entityReference == null)
            return null;

        if (entityReference.Value is T)
            return entityReference;

        switch (entityReference.Value) {
            case Pointer pointer:
                return ExpandWrappers<T>(pointer.ValueType);
            case Typedef typedef:
                return ExpandWrappers<T>(typedef.Type);
        }

        return null;
    }

    public static IEntityReference? ExpandTypedef(this IEntityReference? entityReference) {
        if (entityReference == null)
            return null;

        switch (entityReference.Value) {
            case Typedef typedef:
                return ExpandTypedef(typedef.Type);
        }

        return entityReference;
    }
}

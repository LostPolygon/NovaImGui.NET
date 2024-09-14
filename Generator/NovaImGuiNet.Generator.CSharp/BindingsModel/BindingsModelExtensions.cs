using System.Diagnostics.CodeAnalysis;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;

public static class BindingsModelExtensions {
    public static Pointer AsPointer(this Entity entity, bool? isNullable = null) =>
        new(entity.AsEntityReference(), isNullable);

    public static IEntityReference? ExpandAllTypedefsAndPointers<T>(this IEntityReference? entityReference) where T : Entity {
        if (entityReference == null)
            return null;

        if (entityReference.Value is T)
            return entityReference;

        switch (entityReference.Value) {
            case Pointer pointer:
                return ExpandAllTypedefsAndPointers<T>(pointer.ValueType);
            case Typedef typedef:
                return ExpandAllTypedefsAndPointers<T>(typedef.Type);
        }

        return null;
    }

    [return: NotNullIfNotNull(nameof(entityReference))]
    public static IEntityReference? ExpandAllTypedefsAndPointers(this IEntityReference? entityReference)  {
        if (entityReference == null)
            return null;

        switch (entityReference.Value) {
            case Pointer pointer:
                if (pointer.IsVoidPointer()) {
                    return entityReference;
                }

                return ExpandAllTypedefsAndPointers(pointer.ValueType);
            case Typedef typedef:
                return ExpandAllTypedefsAndPointers(typedef.Type);
        }

        return entityReference;
    }

    [return: NotNullIfNotNull(nameof(entityReference))]
    public static IEntityReference? ExpandPointer(this IEntityReference? entityReference)  {
        if (entityReference == null)
            return null;

        switch (entityReference.Value) {
            case Pointer pointer:
                if (pointer.IsVoidPointer()) {
                    return entityReference;
                }

                return pointer.ValueType;
        }

        return entityReference;
    }

    [return: NotNullIfNotNull(nameof(entityReference))]
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

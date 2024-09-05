namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;

public static class BindingsModelExtensions {
    public static Pointer AsPointer(this Builtin builtin, bool? isNullable = null) => 
        new(builtin.AsEntityReference(), isNullable);

    public static IEntityReference? ExpandTypedefsAndPointers<T>(this IEntityReference? entityReference) where T : Entity {
        if (entityReference == null)
            return null;

        if (entityReference.Value is T)
            return entityReference;

        switch (entityReference.Value) {
            case Pointer pointer:
                return ExpandTypedefsAndPointers<T>(pointer.ValueType);
            case Typedef typedef:
                return ExpandTypedefsAndPointers<T>(typedef.Type);
        }

        return null;
    }

    public static IEntityReference? ExpandTypedefsAndPointers(this IEntityReference? entityReference)  {
        if (entityReference == null)
            return null;

        switch (entityReference.Value) {
            case Pointer pointer:
                if (pointer.ValueType.Value is Builtin { BuiltinName: "void" }) {
                    return entityReference;
                }

                return ExpandTypedefsAndPointers(pointer.ValueType);
            case Typedef typedef:
                return ExpandTypedefsAndPointers(typedef.Type);
        }

        return entityReference;
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

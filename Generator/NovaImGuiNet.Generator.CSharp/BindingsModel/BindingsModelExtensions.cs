namespace NovaImGuiNet.Generator.CSharp;

public static class BindingsModelExtensions {
    public static BindingsModel.IEntityReference AsEntityReference(this BindingsModel.Entity entity) =>
        ((BindingsModel.IConvertibleToEntityReference) entity).AsEntityReference();
}

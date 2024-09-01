namespace NovaImGuiNet.Generator.CSharp;

public static class BindingsModelTransformUtility {
    public static void Transform<TEntity>(
        IList<BindingsModel.IEntityReference> entityReferences,
        Func<TEntity, TEntity?> transformer
    ) where TEntity : BindingsModel.Entity {
        List<BindingsModel.IEntityReference> updated = [];
        foreach (BindingsModel.IEntityReference entityReference in entityReferences) {
            TEntity? value = (TEntity?) entityReference.Value;

            value = transformer(value);
            if (value == null)
                continue;

            entityReference.Value = value;
            updated.Add(entityReference);
        }

        entityReferences.Clear();
        foreach (BindingsModel.IEntityReference updatedEntity in updated) {
            entityReferences.Add(updatedEntity);
        }
    }
}

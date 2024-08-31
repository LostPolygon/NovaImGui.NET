namespace NovaImGuiNet.Generator.CSharp;

public static class BindingsModelTransformUtility {
    public static void Transform<TEntity>(
        IList<BindingsModel.IEntityReference<TEntity>> entityReferences,
        Func<TEntity, TEntity?> transformer
    ) where TEntity : BindingsModel.Entity {
        List<BindingsModel.IEntityReference<TEntity>> updated = [];
        foreach (BindingsModel.IEntityReference<TEntity> entityReference in entityReferences) {
            TEntity? value = entityReference.Value;

            value = transformer(value);
            if (value == null)
                continue;

            entityReference.Value = value;
            updated.Add(entityReference);
        }

        entityReferences.Clear();
        foreach (BindingsModel.IEntityReference<TEntity> updatedEntity in updated) {
            entityReferences.Add(updatedEntity);
        }
    }
}

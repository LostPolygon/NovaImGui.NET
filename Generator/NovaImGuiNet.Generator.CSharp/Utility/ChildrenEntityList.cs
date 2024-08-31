namespace NovaImGuiNet.Generator.CSharp;

public class ChildrenEntityList(IReadOnlyList<Type> allowedItemTypes) :
    ChildrenEntityList<BindingsModel.IEntity>(allowedItemTypes);

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public readonly record struct ManagedMethodArgument(
    BindingsModel.FunctionArgument NativeArgument,
    BindingsModel.FunctionArgument MarshaledArgument,
    NativeFunctionArgumentMarshalingVariant MarshalingVariant,
    bool IsPartOfDeclaration
);

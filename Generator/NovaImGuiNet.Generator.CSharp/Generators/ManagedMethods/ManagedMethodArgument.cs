namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public readonly record struct ManagedMethodArgument(
    BindingsModel.FunctionArgument NativeArgument,
    BindingsModel.FunctionArgument MarshalledArgument,
    NativeFunctionArgumentMarshallingVariant MarshallingVariant,
    bool IsPartOfDeclaration
);

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static BindingsModel;

public readonly record struct ManagedMethodReturnType(
    Entity NativeReturnType,
    Entity MarshalledReturnType,
    NativeFunctionReturnTypeMarshallingVariant MarshallingVariant
);

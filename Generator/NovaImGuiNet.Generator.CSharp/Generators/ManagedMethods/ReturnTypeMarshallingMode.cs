namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public enum ReturnTypeMarshallingMode {
    Void,
    Passthrough,
    NullTerminatedString,
    PointerWrapper,
    Ref
}

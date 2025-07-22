namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public enum ReturnTypeMarshallingMode {
    Void,
    Passthrough,
    NullTerminatedUtf8String,
    PointerWrapper,
    Ref
}

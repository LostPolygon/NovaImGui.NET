namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public enum ArgumentMarshallingMode {
    Passthrough,

    // Pointers
    Ref,
    Out,

    IntPtr,
    IntPtrRef,
    IntPtrOut,

    // Strings
    StringBytePointer,
    StringReadOnlyByteSpan,
    StringReadOnlyCharSpan,
    StringManaged,

    // Delegates
    DelegateManaged,
    DelegateFunctionPointer,
    DelegateIntPtr
}

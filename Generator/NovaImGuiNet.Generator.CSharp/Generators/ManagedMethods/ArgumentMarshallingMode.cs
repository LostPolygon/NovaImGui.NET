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
    ReadOnlyByteSpan,
    ReadOnlyCharSpan,
    String,

    // Delegates
    DelegateManaged,
    DelegateFunctionPointer,
    DelegateIntPtr
}

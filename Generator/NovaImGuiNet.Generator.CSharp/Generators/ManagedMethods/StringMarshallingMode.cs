namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public enum StringMarshallingMode {
    Undefined,

    BytePointer,
    ReadOnlyByteSpan,
    ReadOnlyCharSpan,
    String,
}

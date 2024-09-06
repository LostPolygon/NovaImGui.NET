namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public enum StringMarshalingMode {
    Undefined,

    BytePointer,
    ReadOnlyByteSpan,
    ReadOnlyCharSpan,
    String,
}

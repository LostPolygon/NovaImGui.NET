namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;

public static class KnownEntities {
    public static readonly Builtin String = new("string", typeof(string), StorageClass.Const);
    public static readonly Builtin IntPtr = new("IntPtr", typeof(IntPtr), StorageClass.NotSpecified);
    public static readonly Pointer Utf8BytePointer = new Builtin("byte", typeof(byte), StorageClass.Const).AsPointer();
    public static readonly Builtin ReadOnlySpanByte = new("ReadOnlySpan<byte>", typeof(ReadOnlySpan<byte>), StorageClass.Const);
    public static readonly Builtin ReadOnlySpanChar = new("ReadOnlySpan<char>", typeof(ReadOnlySpan<char>), StorageClass.Const);
}

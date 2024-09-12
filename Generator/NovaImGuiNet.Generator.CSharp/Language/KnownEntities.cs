namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;

public static class KnownEntities {
    public static readonly Builtin String = new("string", typeof(string), StorageClass.Const);
    public static readonly Builtin IntPtr = new("IntPtr", typeof(IntPtr), StorageClass.NotSpecified);
    public static readonly Pointer Utf8BytePointer = new Builtin("byte", typeof(byte), StorageClass.Const).AsPointer();
    public static readonly Builtin ReadOnlySpanByte = new("ReadOnlySpan<byte>", typeof(ReadOnlySpan<byte>), StorageClass.Const);
    public static readonly Builtin ReadOnlySpanChar = new("ReadOnlySpan<char>", typeof(ReadOnlySpan<char>), StorageClass.Const);
    public static readonly Pointer VoidPointer = new Builtin("void", typeof(void), StorageClass.NotSpecified).AsPointer();
    public static readonly Pointer ConstVoidPointer = new Builtin("void", typeof(void), StorageClass.Const).AsPointer();

    public static bool IsVoidPointer(this Entity entity) {
        if (entity is not Pointer pointer)
            return false;

        return
            pointer.ValueType.Equals(VoidPointer.ValueType) ||
            pointer.ValueType.Equals(ConstVoidPointer.ValueType);
    }

    public static bool IsConstCharPointer(this Entity entity) =>
        entity is Pointer {
            ValueType.Value: Builtin { IsConstCharPointer: true }
        };

    public static bool IsConstCharPointer(this IEntityReference entityReference) =>
        IsConstCharPointer(entityReference.ExpandTypedef()!.Value);
}

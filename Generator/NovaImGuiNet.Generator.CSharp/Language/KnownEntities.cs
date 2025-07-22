namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;

public static class KnownEntities {
    public static readonly Builtin Byte = new("byte", typeof(byte), StorageClass.NotSpecified);
    public static readonly Builtin String = new("string", typeof(string), StorageClass.Const);
    public static readonly Builtin IntPtr = new("IntPtr", typeof(IntPtr), StorageClass.NotSpecified);
    public static readonly Pointer Utf8BytePointer = new Builtin("byte", typeof(byte), StorageClass.Const).AsPointer();
    public static readonly Builtin ReadOnlySpanByte = new("ReadOnlySpan<byte>", typeof(ReadOnlySpan<byte>), StorageClass.Const);
    public static readonly Builtin ReadOnlySpanChar = new("ReadOnlySpan<char>", typeof(ReadOnlySpan<char>), StorageClass.Const);
    public static readonly Builtin ImGuiUtf8StringWriter = new("ImGuiUtf8StringWriter", null, StorageClass.NotSpecified);
    public static readonly Builtin NullTerminatedUtf8String = new ("NullTerminatedUtf8String", null, StorageClass.NotSpecified);
    public static readonly Pointer VoidPointer = new Builtin("void", typeof(void), StorageClass.NotSpecified).AsPointer();
    public static readonly Pointer VoidVoidPointer = new Builtin("void", typeof(void), StorageClass.NotSpecified).AsPointer().AsPointer();
    public static readonly Pointer ConstVoidPointer = new Builtin("void", typeof(void), StorageClass.Const).AsPointer();

    public static readonly Builtin ByteBool = new("ByteBool", null, StorageClass.NotSpecified);
    public static readonly IReadOnlyList<string> NullishValues = ["default", "null"];
    public static readonly IReadOnlyList<string> PotentiallyAmbiguousDefaultValues = ["default", "null"];

    public static bool IsVoid(this Entity entity) => entity is Builtin builtin && builtin.ManagedType == typeof(void);

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

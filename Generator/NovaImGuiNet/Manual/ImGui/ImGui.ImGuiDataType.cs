#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImGuiDataType GetImGuiDataType<T>() where T : unmanaged =>
        GetImGuiDataType(typeof(T));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImGuiDataType GetImGuiDataType(Type type) =>
        GetImGuiDataType(Type.GetTypeCode(type));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImGuiDataType GetImGuiDataType(TypeCode typeCode) =>
        typeCode switch {
            TypeCode.Boolean => ImGuiDataType.Bool,
            TypeCode.SByte => ImGuiDataType.S8,
            TypeCode.Byte => ImGuiDataType.U8,
            TypeCode.Int16 => ImGuiDataType.S16,
            TypeCode.UInt16 => ImGuiDataType.U16,
            TypeCode.Int32 => ImGuiDataType.S32,
            TypeCode.UInt32 => ImGuiDataType.U32,
            TypeCode.Int64 => ImGuiDataType.S64,
            TypeCode.UInt64 => ImGuiDataType.U64,
            TypeCode.Single => ImGuiDataType.Float,
            TypeCode.Double => ImGuiDataType.Double,
            _ => throw new ArgumentOutOfRangeException()
        };
}

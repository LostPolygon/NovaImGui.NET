#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ImGuiDataType GetImGuiDataType<T>() where T : unmanaged {
        if (typeof(T) == typeof(Boolean)) return ImGuiDataType.Bool;
        if (typeof(T) == typeof(SByte)) return ImGuiDataType.S8;
        if (typeof(T) == typeof(Byte)) return ImGuiDataType.U8;
        if (typeof(T) == typeof(Int16)) return ImGuiDataType.S16;
        if (typeof(T) == typeof(UInt16)) return ImGuiDataType.U16;
        if (typeof(T) == typeof(Int32)) return ImGuiDataType.S32;
        if (typeof(T) == typeof(UInt32)) return ImGuiDataType.U32;
        if (typeof(T) == typeof(Int64)) return ImGuiDataType.S64;
        if (typeof(T) == typeof(UInt64)) return ImGuiDataType.U64;
        if (typeof(T) == typeof(Single)) return ImGuiDataType.Float;
        if (typeof(T) == typeof(Double)) return ImGuiDataType.Double;

        throw new ArgumentOutOfRangeException(nameof(T), typeof(T), "Data type not supported by Dear ImGui");
    }
}

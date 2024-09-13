#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    #region VSliderScalar

    public static bool VSliderScalar<T>(
        ReadOnlySpan<byte> label,
        ImVec2 size,
        ref T data,
        T min = default,
        T max = default,
        ReadOnlySpan<byte> format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return VSliderScalar(label, size, dataType, (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool VSliderScalar<T>(
        ReadOnlySpan<char> label,
        ImVec2 size,
        ref T data,
        T min = default,
        T max = default,
        ReadOnlySpan<char> format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return VSliderScalar(label, size, dataType, (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool VSliderScalar<T>(
        ReadOnlySpan<char> label,
        ImVec2 size,
        ref T data,
        T min = default,
        T max = default,
        ReadOnlySpan<byte> format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return VSliderScalar(label, size, dataType, (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    #endregion
}

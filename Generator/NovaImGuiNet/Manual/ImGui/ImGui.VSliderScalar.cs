#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    #region VSliderScalar

    public static bool VSliderScalar<T>(
        ReadOnlySpan<byte> label,
        ImVec2 size,
        ref T data,
        T min,
        T max,
        ReadOnlySpan<byte> format,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        return VSliderScalar(label, size, GetImGuiDataType<T>(), (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool VSliderScalar<T>(
        ReadOnlySpan<char> label,
        ImVec2 size,
        ref T data,
        T min,
        T max,
        ReadOnlySpan<char> format,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        return VSliderScalar(label, size, GetImGuiDataType<T>(), (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool VSliderScalar<T>(
        ReadOnlySpan<char> label,
        ImVec2 size,
        ref T data,
        T min,
        T max,
        ReadOnlySpan<byte> format,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        return VSliderScalar(label, size, GetImGuiDataType<T>(), (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool VSliderScalar<T>(
        ReadOnlySpan<byte> label,
        ImVec2 size,
        ref T data,
        T min = default,
        T max = default
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        return VSliderScalar(label, size, GetImGuiDataType<T>(), (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max));
    }

    public static bool VSliderScalar<T>(
        ReadOnlySpan<char> label,
        ImVec2 size,
        ref T data,
        T min = default,
        T max = default
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        return VSliderScalar(label, size, GetImGuiDataType<T>(), (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max));
    }

    #endregion
}

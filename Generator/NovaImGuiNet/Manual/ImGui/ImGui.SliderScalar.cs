#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    #region SliderScalar

    public static bool SliderScalar<T>(
        ReadOnlySpan<byte> label,
        ref T data,
        T min = default,
        T max = default,
        ReadOnlySpan<byte> format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return SliderScalar(label, dataType, (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool SliderScalar<T>(
        ReadOnlySpan<char> label,
        ref T data,
        T min = default,
        T max = default,
        ReadOnlySpan<char> format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return SliderScalar(label, dataType, (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool SliderScalar<T>(
        ReadOnlySpan<char> label,
        ref T data,
        T min = default,
        T max = default,
        ReadOnlySpan<byte> format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return SliderScalar(label, dataType, (IntPtr) dataPtr, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    #endregion

    #region SliderScalarN

    public static bool SliderScalarN<T>(
        ReadOnlySpan<byte> label,
        Span<T> data,
        T min = default,
        T max = default,
        ReadOnlySpan<byte> format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return SliderScalarN(label, dataType, (IntPtr) dataPtr, data.Length, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool SliderScalarN<T>(
        ReadOnlySpan<char> label,
        Span<T> data,
        T min = default,
        T max = default,
        ReadOnlySpan<char> format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return SliderScalarN(label, dataType, (IntPtr) dataPtr, data.Length, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool SliderScalarN<T>(
        ReadOnlySpan<char> label,
        Span<T> data,
        T min = default,
        T max = default,
        ReadOnlySpan<byte> format = default,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return SliderScalarN(label, dataType, (IntPtr) dataPtr, data.Length, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    #endregion
}

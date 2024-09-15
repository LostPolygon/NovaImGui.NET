#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    #region DragScalar

    public static bool DragScalar<T>(
        ReadOnlySpan<byte> label,
        ref T data,
        float vSpeed,
        T min,
        T max,
        ReadOnlySpan<byte> format,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return DragScalar(label, dataType, (IntPtr) dataPtr, vSpeed, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool DragScalar<T>(
        ReadOnlySpan<char> label,
        ref T data,
        float vSpeed,
        T min,
        T max,
        ReadOnlySpan<char> format,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return DragScalar(label, dataType, (IntPtr) dataPtr, vSpeed, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool DragScalar<T>(
        ReadOnlySpan<char> label,
        ref T data,
        float vSpeed,
        T min,
        T max,
        ReadOnlySpan<byte> format,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return DragScalar(label, dataType, (IntPtr) dataPtr, vSpeed, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool DragScalar<T>(
        ReadOnlySpan<byte> label,
        ref T data,
        float vSpeed = 1.0f,
        T min = default,
        T max = default
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return DragScalar(label, dataType, (IntPtr) dataPtr, vSpeed, (IntPtr) (&min), (IntPtr) (&max));
    }

    public static bool DragScalar<T>(
        ReadOnlySpan<char> label,
        ref T data,
        float vSpeed = 1.0f,
        T min = default,
        T max = default
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return DragScalar(label, dataType, (IntPtr) dataPtr, vSpeed, (IntPtr) (&min), (IntPtr) (&max));
    }

    #endregion

    #region DragScalarN

    public static bool DragScalarN<T>(
        ReadOnlySpan<byte> label,
        Span<T> data,
        float vSpeed,
        T min,
        T max,
        ReadOnlySpan<byte> format,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return DragScalarN(label, dataType, (IntPtr) dataPtr, data.Length, vSpeed, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool DragScalarN<T>(
        ReadOnlySpan<char> label,
        Span<T> data,
        float vSpeed,
        T min,
        T max,
        ReadOnlySpan<char> format,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return DragScalarN(label, dataType, (IntPtr) dataPtr, data.Length, vSpeed, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool DragScalarN<T>(
        ReadOnlySpan<char> label,
        Span<T> data,
        float vSpeed,
        T min,
        T max,
        ReadOnlySpan<byte> format,
        ImGuiSliderFlags flags = ImGuiSliderFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return DragScalarN(label, dataType, (IntPtr) dataPtr, data.Length, vSpeed, (IntPtr) (&min), (IntPtr) (&max), format, flags);
    }

    public static bool DragScalarN<T>(
        ReadOnlySpan<byte> label,
        Span<T> data,
        float vSpeed = 1.0f,
        T min = default,
        T max = default
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return DragScalarN(label, dataType, (IntPtr) dataPtr, data.Length, vSpeed, (IntPtr) (&min), (IntPtr) (&max));
    }

    public static bool DragScalarN<T>(
        ReadOnlySpan<char> label,
        Span<T> data,
        float vSpeed = 1.0f,
        T min = default,
        T max = default
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return DragScalarN(label, dataType, (IntPtr) dataPtr, data.Length, vSpeed, (IntPtr) (&min), (IntPtr) (&max));
    }

    #endregion
}

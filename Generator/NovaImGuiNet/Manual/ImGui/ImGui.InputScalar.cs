#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    #region InputScalar

    public static bool InputScalar<T>(
        ReadOnlySpan<byte> label,
        ref T data,
        T step,
        T stepFast,
        ReadOnlySpan<byte> format,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        return InputScalar(label, GetImGuiDataType<T>(), (IntPtr) dataPtr, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    public static bool InputScalar<T>(
        ReadOnlySpan<char> label,
        ref T data,
        T step,
        T stepFast,
        ReadOnlySpan<char> format,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        return InputScalar(label, GetImGuiDataType<T>(), (IntPtr) dataPtr, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    public static bool InputScalar<T>(
        ReadOnlySpan<char> label,
        ref T data,
        T step,
        T stepFast,
        ReadOnlySpan<byte> format,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        return InputScalar(label, GetImGuiDataType<T>(), (IntPtr) dataPtr, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    public static bool InputScalar<T>(
        ReadOnlySpan<byte> label,
        ref T data,
        T step = default,
        T stepFast = default
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        return InputScalar(label, GetImGuiDataType<T>(), (IntPtr) dataPtr, (IntPtr) (&step), (IntPtr) (&stepFast));
    }

    public static bool InputScalar<T>(
        ReadOnlySpan<char> label,
        ref T data,
        T step = default,
        T stepFast = default
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        return InputScalar(label, GetImGuiDataType<T>(), (IntPtr) dataPtr, (IntPtr) (&step), (IntPtr) (&stepFast));
    }

    #endregion

    #region InputScalarN

    public static bool InputScalarN<T>(
        ReadOnlySpan<byte> label,
        Span<T> data,
        T step,
        T stepFast,
        ReadOnlySpan<byte> format,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        return InputScalarN(label, GetImGuiDataType<T>(), (IntPtr) dataPtr, data.Length, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    public static bool InputScalarN<T>(
        ReadOnlySpan<char> label,
        Span<T> data,
        T step,
        T stepFast,
        ReadOnlySpan<char> format,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        return InputScalarN(label, GetImGuiDataType<T>(), (IntPtr) dataPtr, data.Length, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    public static bool InputScalarN<T>(
        ReadOnlySpan<char> label,
        Span<T> data,
        T step,
        T stepFast,
        ReadOnlySpan<byte> format,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        return InputScalarN(label, GetImGuiDataType<T>(), (IntPtr) dataPtr, data.Length, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    public static bool InputScalarN<T>(
        ReadOnlySpan<byte> label,
        Span<T> data,
        T step = default,
        T stepFast = default
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        return InputScalarN(label, GetImGuiDataType<T>(), (IntPtr) dataPtr, data.Length, (IntPtr) (&step), (IntPtr) (&stepFast));
    }

    public static bool InputScalarN<T>(
        ReadOnlySpan<char> label,
        Span<T> data,
        T step = default,
        T stepFast = default
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        return InputScalarN(label, GetImGuiDataType<T>(), (IntPtr) dataPtr, data.Length, (IntPtr) (&step), (IntPtr) (&stepFast));
    }

    #endregion
}

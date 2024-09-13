#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    #region InputScalar

    public static bool InputScalar<T>(
        ReadOnlySpan<byte> label,
        ref T data,
        T step = default,
        T stepFast = default,
        ReadOnlySpan<byte> format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return InputScalar(label, dataType, (IntPtr) dataPtr, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    public static bool InputScalar<T>(
        ReadOnlySpan<char> label,
        ref T data,
        T step = default,
        T stepFast = default,
        ReadOnlySpan<char> format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return InputScalar(label, dataType, (IntPtr) dataPtr, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    public static bool InputScalar<T>(
        ReadOnlySpan<char> label,
        ref T data,
        T step = default,
        T stepFast = default,
        ReadOnlySpan<byte> format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = (T*) Unsafe.AsPointer(ref data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return InputScalar(label, dataType, (IntPtr) dataPtr, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    #endregion

    #region InputScalarN

    public static bool InputScalarN<T>(
        ReadOnlySpan<byte> label,
        Span<T> data,
        T step = default,
        T stepFast = default,
        ReadOnlySpan<byte> format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return InputScalarN(label, dataType, (IntPtr) dataPtr, data.Length, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    public static bool InputScalarN<T>(
        ReadOnlySpan<char> label,
        Span<T> data,
        T step = default,
        T stepFast = default,
        ReadOnlySpan<char> format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return InputScalarN(label, dataType, (IntPtr) dataPtr, data.Length, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    public static bool InputScalarN<T>(
        ReadOnlySpan<char> label,
        Span<T> data,
        T step = default,
        T stepFast = default,
        ReadOnlySpan<byte> format = default,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None
    ) where T : unmanaged {
        T* dataPtr = InteropUtility.SpanToPointer<T>(data);
        ImGuiDataType dataType = GetImGuiDataType<T>();
        return InputScalarN(label, dataType, (IntPtr) dataPtr, data.Length, (IntPtr) (&step), (IntPtr) (&stepFast), format, flags);
    }

    #endregion
}

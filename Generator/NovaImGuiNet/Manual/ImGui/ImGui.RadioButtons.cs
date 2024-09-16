#nullable enable

namespace NovaImGuiNet;

public static partial class ImGui {
    public static bool RadioButton<TEnum>(ReadOnlySpan<byte> label, ref TEnum v, TEnum vButton)
        where TEnum : unmanaged, Enum {
        Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));

        if (underlyingType == typeof(int)) {
            ref int vInt = ref Unsafe.As<TEnum, int>(ref v);
            int vButtonInt = Unsafe.As<TEnum, int>(ref vButton);
            return RadioButton(label, ref vInt, vButtonInt);
        }

        if (underlyingType == typeof(int)) {
            ref int vUint = ref Unsafe.As<TEnum, int>(ref v);
            int vButtonUint = Unsafe.As<TEnum, int>(ref vButton);
            return RadioButton(label, ref vUint, vButtonUint);
        }

        throw CommonExceptions.CreateUnsupportedEnumUnderlyingType<TEnum>();
    }

    public static bool RadioButton<TEnum>(ReadOnlySpan<char> label, ref TEnum v, TEnum vButton)
        where TEnum : unmanaged, Enum {
        Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));

        if (underlyingType == typeof(int)) {
            ref int vInt = ref Unsafe.As<TEnum, int>(ref v);
            int vButtonInt = Unsafe.As<TEnum, int>(ref vButton);
            return RadioButton(label, ref vInt, vButtonInt);
        }

        if (underlyingType == typeof(int)) {
            ref int vUint = ref Unsafe.As<TEnum, int>(ref v);
            int vButtonUint = Unsafe.As<TEnum, int>(ref vButton);
            return RadioButton(label, ref vUint, vButtonUint);
        }

        throw CommonExceptions.CreateUnsupportedEnumUnderlyingType<TEnum>();
    }

#if NETSTANDARD2_0
    public static bool RadioButton<TEnum>(string label, ref TEnum v, TEnum vButton)
        where TEnum : unmanaged, Enum {
        Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));

        if (underlyingType == typeof(int)) {
            ref int vInt = ref Unsafe.As<TEnum, int>(ref v);
            int vButtonInt = Unsafe.As<TEnum, int>(ref vButton);
            return RadioButton(label, ref vInt, vButtonInt);
        }

        if (underlyingType == typeof(int)) {
            ref int vUint = ref Unsafe.As<TEnum, int>(ref v);
            int vButtonUint = Unsafe.As<TEnum, int>(ref vButton);
            return RadioButton(label, ref vUint, vButtonUint);
        }

        throw CommonExceptions.CreateUnsupportedEnumUnderlyingType<TEnum>();
    }
#endif
}

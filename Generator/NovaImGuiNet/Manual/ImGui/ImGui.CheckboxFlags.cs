#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    public static bool CheckboxFlags<TEnum>(ReadOnlySpan<byte> label, ref TEnum flags, TEnum flagsValue)
        where TEnum : unmanaged, Enum {
        Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));

        if (underlyingType == typeof(int)) {
            ref int flagsInt = ref Unsafe.As<TEnum, int>(ref flags);
            int flagsValueInt = Unsafe.As<TEnum, int>(ref flagsValue);
            return CheckboxFlags(label, ref flagsInt, flagsValueInt);
        }

        if (underlyingType == typeof(int)) {
            ref uint flagsUint = ref Unsafe.As<TEnum, uint>(ref flags);
            uint flagsValueUint = Unsafe.As<TEnum, uint>(ref flagsValue);
            return CheckboxFlags(label, ref flagsUint, flagsValueUint);
        }

        throw new InvalidCastException(
            "Only enums with int and uint underlying types are supported, got type: " + underlyingType
        );
    }

    public static bool CheckboxFlags<TEnum>(ReadOnlySpan<char> label, ref TEnum flags, TEnum flagsValue)
        where TEnum : unmanaged, Enum {
        Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));

        if (underlyingType == typeof(int)) {
            ref int flagsInt = ref Unsafe.As<TEnum, int>(ref flags);
            int flagsValueInt = Unsafe.As<TEnum, int>(ref flagsValue);
            return CheckboxFlags(label, ref flagsInt, flagsValueInt);
        }

        if (underlyingType == typeof(int)) {
            ref uint flagsUint = ref Unsafe.As<TEnum, uint>(ref flags);
            uint flagsValueUint = Unsafe.As<TEnum, uint>(ref flagsValue);
            return CheckboxFlags(label, ref flagsUint, flagsValueUint);
        }

        throw new InvalidCastException(
            "Only enums with int and uint underlying types are supported, got type: " + underlyingType
        );
    }

#if NETSTANDARD2_0
    public static bool CheckboxFlags<TEnum>(string label, ref TEnum flags, TEnum flagsValue)
        where TEnum : unmanaged, Enum {
        Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));

        if (underlyingType == typeof(int)) {
            ref int flagsInt = ref Unsafe.As<TEnum, int>(ref flags);
            int flagsValueInt = Unsafe.As<TEnum, int>(ref flagsValue);
            return CheckboxFlags(label.AsSpan(), ref flagsInt, flagsValueInt);
        }

        if (underlyingType == typeof(int)) {
            ref uint flagsUint = ref Unsafe.As<TEnum, uint>(ref flags);
            uint flagsValueUint = Unsafe.As<TEnum, uint>(ref flagsValue);
            return CheckboxFlags(label.AsSpan(), ref flagsUint, flagsValueUint);
        }

        throw new InvalidCastException(
            "Only enums with int and uint underlying types are supported, got type: " + underlyingType
        );
    }
#endif
}

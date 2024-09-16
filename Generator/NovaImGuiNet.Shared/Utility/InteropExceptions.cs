namespace NovaImGuiNet.Interop;

public static class InteropExceptions {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowReturnedNativePointerNull(
        string? customCallerName = null,
        [CallerMemberName] string callerMemberName = ""
    ) {
        customCallerName ??= callerMemberName;
        throw new NullReferenceException(
            $"Native pointer returned for '{customCallerName}' method was null, " +
            $"which is not allowed. Make sure your code is correct. " +
            $"If you need a raw pointer, use the '{customCallerName}Pointer' method instead."
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowNativePointerNull<T>() {
        throw new NullReferenceException(
            $"Native pointer was null, and it is not allowed to get a ref from a null pointer. " +
            $"Make sure your code is correct. " +
            $"If you need a raw pointer, use the 'Pointer<{typeof(T).Name}>.Ptr' field instead."
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Exception CreateUnsupportedEnumUnderlyingType<TEnum>() where TEnum : unmanaged, Enum {
        return new InvalidCastException(
            $"Only enums with int and uint underlying types are supported, " +
            $"but enum {typeof(TEnum).FullName} uses underlying type {Enum.GetUnderlyingType(typeof(TEnum)).FullName}"
        );
    }
}

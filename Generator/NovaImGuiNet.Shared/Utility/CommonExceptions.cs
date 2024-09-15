namespace NovaImGuiNet.Interop;

internal static class CommonExceptions {
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
        string pointerWrapperTypeName = $"Pointer<{typeof(T).Name}>";

        throw new NullReferenceException(
            $"Native pointer was null, and it is not allowed to get a ref from a null pointer. " +
            $"Make sure your code is correct. " +
            $"If you need a raw pointer, use the '{pointerWrapperTypeName}.Ptr' field instead."
        );
    }
}

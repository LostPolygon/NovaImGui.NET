using System.Diagnostics.CodeAnalysis;

namespace NovaImGuiNet.Interop;

public static unsafe class UnsafeUtility {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* RefInToPointer<T>(in T value) where T : unmanaged {
        return (T*) Unsafe.AsPointer(ref Unsafe.AsRef(in value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* SpanToPointer<T>(Span<T> span) where T : unmanaged {
        return (T*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* SpanToPointer<T>(ReadOnlySpan<T> span) where T : unmanaged {
        return (T*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreSame<T>([AllowNull] ref readonly T left, [AllowNull] ref readonly T right)
    {
        throw new PlatformNotSupportedException();
        //InlineIL.IL.Emit.Ldarg_0();
        //InlineIL.IL.Emit.Ret();
        //throw InlineIL.IL.Unreachable();
        // ldarg.0
        // ldarg.1
        // ceq
        // ret
    }
}

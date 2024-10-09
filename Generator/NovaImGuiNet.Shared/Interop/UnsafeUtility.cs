using System.Diagnostics.CodeAnalysis;

namespace NovaImGuiNet.Interop;

public static unsafe class UnsafeUtility {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* RefInToPointer<T>(in T value) where T : unmanaged {
        InlineIL.IL.Emit.Ldarg_0();
        InlineIL.IL.Emit.Conv_U();
        InlineIL.IL.Emit.Ret();
        throw InlineIL.IL.Unreachable();

        //return (T*) Unsafe.AsPointer(ref Unsafe.AsRef(in value));
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
    public static bool AreSame<T>([AllowNull] ref readonly T left, [AllowNull] ref readonly T right) {
        InlineIL.IL.Emit.Ldarg_0();
        InlineIL.IL.Emit.Ldarg_1();
        InlineIL.IL.Emit.Ceq();
        InlineIL.IL.Emit.Ret();
        throw InlineIL.IL.Unreachable();
    }
}

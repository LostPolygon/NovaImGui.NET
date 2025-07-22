using System.Diagnostics.CodeAnalysis;

namespace NovaImGuiNet.Interop;

public static unsafe class UnsafeUtility {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* AsPointer<T>(in T value) where T : unmanaged {
        InlineIL.IL.Emit.Ldarg_0();
        InlineIL.IL.Emit.Conv_U();
        return InlineIL.IL.ReturnPointer<T>();
        //return (T*) Unsafe.AsPointer(ref Unsafe.AsRef(in value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T** AsPointer<T>(in T* value) where T : unmanaged {
        InlineIL.IL.Emit.Ldarg_0();
        InlineIL.IL.Emit.Conv_U();
        return (T**) InlineIL.IL.ReturnPointer();
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
    public static Span<T> CreateSpan<T>(scoped ref T reference, int length) =>
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        MemoryMarshal.CreateSpan(ref reference, length);
#else
        new Span<T>(Unsafe.AsPointer(ref reference), length);
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<Pointer<T>> CreateSpan<T>(scoped ref T* reference, int length) where T : unmanaged {
        Span<IntPtr> intPtrSpan = CreateSpan(ref AsIntPtrRef(ref reference), length);
        return MemoryMarshal.Cast<IntPtr, Pointer<T>>(intPtrSpan);
    }

    // ReSharper disable once UnusedParameter.Global
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreRefSame<T>([AllowNull] ref readonly T left, [AllowNull] ref readonly T right) {
        InlineIL.IL.Emit.Ldarg_0();
        InlineIL.IL.Emit.Ldarg_1();
        InlineIL.IL.Emit.Ceq();
        InlineIL.IL.Emit.Ret();
        throw InlineIL.IL.Unreachable();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref IntPtr AsIntPtrRef<T>(ref readonly T* ptr) where T : unmanaged {
        InlineIL.IL.Emit.Ldarg_0();
        InlineIL.IL.Emit.Ret();
        throw InlineIL.IL.Unreachable();
    }
}

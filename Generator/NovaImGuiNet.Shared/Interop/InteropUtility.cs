namespace NovaImGuiNet.Interop;

public static unsafe class InteropUtility {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* RefInToPointer<T>(in T value) where T : unmanaged {
        return (T*) Unsafe.AsPointer(ref Unsafe.AsRef(in value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* SpanToPointer<T>(ReadOnlySpan<T> span) where T : unmanaged {
        return (T*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<Pointer<T>> CreatePointerArraySpan<T>(ref T* startPtr, int size) where T : unmanaged {
        Span<Pointer<T>> span;

#if false && NET6_0_OR_GREATER
        IntPtr startIntPtr = (IntPtr) startPtr;
        ref Pointer<T> wrappedStartPointer = ref Unsafe.As<IntPtr, Pointer<T>>(ref startIntPtr);
        span = new(wrappedStartPointer, size);
#else
        fixed (T** startPtrPtr = &startPtr) {
            span = new(startPtrPtr, size);
        }
#endif

        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<Pointer<Pointer<T>>> CreatePointerArraySpan<T>(ref T** startPtr, int size) where T : unmanaged {
        Span<Pointer<Pointer<T>>> span;

#if false && NET6_0_OR_GREATER
        IntPtr startIntPtr = (IntPtr) startPtr;
        ref Pointer<Pointer<T>> wrappedStartPointer = ref Unsafe.As<IntPtr, Pointer<Pointer<T>>>(ref startIntPtr);
        span = new(wrappedStartPointer, size);
#else
        fixed (T*** startPtrPtr = &startPtr) {
            span = new(startPtrPtr, size);
        }
#endif

        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint NativeMemoryReallocate(nint ptr, nuint byteCount) {
#if NET6_0_OR_GREATER
        return (IntPtr) NativeMemory.Realloc((void*) ptr, byteCount);
#else
        if (ptr == IntPtr.Zero) {
            return Marshal.AllocHGlobal((nint) byteCount);
        } else {
            return Marshal.ReAllocHGlobal(ptr, (nint) byteCount);
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint NativeMemoryAllocate(nuint byteCount) {
#if NET6_0_OR_GREATER
        return (IntPtr) NativeMemory.Alloc(byteCount);
#else
        return Marshal.AllocHGlobal((nint) byteCount);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void NativeMemoryFree(nint pointer) {
#if NET6_0_OR_GREATER
        NativeMemory.Free((void*) pointer);
#else
        Marshal.FreeHGlobal(pointer);
#endif
    }
}

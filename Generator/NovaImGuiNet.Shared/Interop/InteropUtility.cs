using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace NovaImGuiNet.Interop;

public static unsafe class InteropUtility {
    public const int StackallocSizeLimit = 2048;

    [FixedAddressValueType]
    private static readonly byte EmptyUtf8String;

    [FixedAddressValueType]
    public static readonly byte* EmptyUtf8StringPointer;

    static InteropUtility() {
        fixed (byte* emptyUtf8StringPtr = &EmptyUtf8String) {
            EmptyUtf8StringPointer = emptyUtf8StringPtr;
        }
    }

    /// <summary>
    /// Gets a UTF-8 string memory pointer from <paramref name="stringSpan"/> or
    /// the amount of memory needed for the UTF16->UTF8 string if it's small enough.
    /// </summary>
    /// <param name="stringSpan"></param>
    /// <param name="stackAllocBytes">Size of memory required for converting the string to UTF-8</param>
    /// <param name="pooledMemoryPin">
    /// Handle to the region of memory holding the UTF-8 string.
    /// Needs to be released after the returned pointer is not in use.
    /// </param>
    /// <returns>Pointer to UTF-8 character string, or null if the memory needs to be stack-allocated.</returns>
    public static byte* GetStringUtf8PointerOrStackallocSize(
        in StringSpan stringSpan,
        out int stackAllocBytes,
        out MemoryHandle pooledMemoryPin
    ) {
        stackAllocBytes = 0;
        pooledMemoryPin = default;

        if (stringSpan.IsUtf8)
            return stringSpan.Utf8Pointer;

        ReadOnlySpan<char> utf16 = stringSpan.CharSpan;

        // This pretty much always overestimates the size,
        // but saves on a relatively expensive call to Encoding.UTF8.GetByteCount
        int utf8MaxBytes = Encoding.UTF8.GetMaxByteCount(utf16.Length) + 1;

        bool stackAllocated = utf8MaxBytes <= StackallocSizeLimit;
        if (stackAllocated) {
            stackAllocBytes = utf8MaxBytes;
            return null;
        }

        IMemoryOwner<byte> pooledMemoryOwner = MemoryPool<byte>.Shared.Rent(utf8MaxBytes);
        Memory<byte> pooledMemory = pooledMemoryOwner.Memory;
        pooledMemoryPin = pooledMemory.Pin();
        Utf16SpanToUtf8SpanPointer(utf16, pooledMemory.Span);
        byte* utf8Memory = (byte*) pooledMemoryPin.Pointer;
        return utf8Memory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* Utf16SpanToUtf8SpanPointer(ReadOnlySpan<char> utf16Source, Span<byte> utf8Destination) {
#if NETSTANDARD2_0
        int utf8BytesWritten;
        fixed (char* utf16SourcePtr = &utf16Source.GetPinnableReference())
            fixed (byte* utf8DestinationPtr = &utf8Destination.GetPinnableReference())
                utf8BytesWritten = Encoding.UTF8.GetBytes(
                    utf16SourcePtr,
                    utf16Source.Length,
                    utf8DestinationPtr,
                    utf8Destination.Length
                );
#else
        int utf8BytesWritten = Encoding.UTF8.GetBytes(utf16Source, utf8Destination);
#endif
        utf8Destination[utf8BytesWritten] = 0;

        return SpanToPointer<byte>(utf8Destination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T* SpanToPointer<T>(ReadOnlySpan<T> span) where T : unmanaged {
#if false && NET6_0_OR_GREATER
        return (T*) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
#else
        fixed (T* spanPtr = &span.GetPinnableReference()) {
            return spanPtr;
        }
#endif
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
}

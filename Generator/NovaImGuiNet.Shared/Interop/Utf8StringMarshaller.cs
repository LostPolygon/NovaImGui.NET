using System.ComponentModel.Design;
using System.Text;

namespace NovaImGuiNet.Interop;

public static unsafe class Utf8StringMarshaller {
    public const int StackAllocMaxSize = 2 * 1024;

    [ThreadStatic]
    private static NativeMemoryManagedBuffer? ThreadUtf8ConvertBuffer;

    private const int ThreadUtf8ConvertBufferInitialSize = 128 * 1024;
    private const int ThreadUtf8ConvertBufferMaxSize = 4 * ThreadUtf8ConvertBufferInitialSize;
    private const int ThreadUtf8ConvertBufferGrowthChunk = 4096;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EstimateNullTerminatedUtf8StringByteCount(int charCount) {
        // This pretty much always overestimates the size,
        // but saves on a relatively expensive call to Encoding.UTF8.GetByteCount

        //https://github.com/dotnet/runtime/blob/54280788590d6012758302d4056aa45720133be2/src/libraries/System.Private.CoreLib/src/System/Text/UTF8Encoding.cs#L845
        // +1*3 to account for captured surrogate
        // +1 for null terminator
        const int maxUtf8BytesPerChar = 3;
        return charCount * maxUtf8BytesPerChar + 4;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ConvertUtf16PointerToNullTerminatedUtf8Pointer(char* chars, int charCount, byte* bytes, int byteCount) {
        int utf8BytesWritten = Encoding.UTF8.GetBytes(chars, charCount, bytes, byteCount);
        bytes[utf8BytesWritten] = 0;

        return utf8BytesWritten;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte* GetUtf8Buffer(
        Span<byte> utf8Buffer,
        int utf8ByteCount,
        ref byte* heapUtf8Pointer,
        bool allowUseLongTermBuffer
    ) {
        // Check if the stack-allocated buffer is enough
        bool isStackBufferEnough = utf8ByteCount <= utf8Buffer.Length;
        if (isStackBufferEnough) {
            byte* utf8Pointer = InteropUtility.SpanToPointer<byte>(utf8Buffer);
            return utf8Pointer;
        }

        // If the stack-allocated buffer is not enough,
        // use the long-term heap-allocated buffer,
        // up to a max size limit
        if (allowUseLongTermBuffer && utf8ByteCount <= ThreadUtf8ConvertBufferMaxSize) {
            // Grow the buffer in chunks
            utf8ByteCount = (utf8ByteCount / ThreadUtf8ConvertBufferGrowthChunk + 1) * ThreadUtf8ConvertBufferGrowthChunk;
            if (ThreadUtf8ConvertBuffer == null) {
                ThreadUtf8ConvertBuffer =
                    new NativeMemoryManagedBuffer(
                        (nuint) Math.Max(ThreadUtf8ConvertBufferInitialSize, utf8ByteCount)
                    );
            } else {
                ThreadUtf8ConvertBuffer.EnsureSize((nuint) utf8ByteCount);
            }

            return (byte*) ThreadUtf8ConvertBuffer.MemoryPtr;
        }

        // If the data size exceeds both previous limits,
        // fall back to heap-allocating a short-term buffer

        // serhiiy: this line reduces heap memory usage,
        // but takes a while to execute, reducing the overall performance
        /*utf8ByteCount = Encoding.UTF8.GetByteCount(utf16) + 1;*/

        heapUtf8Pointer = (byte*) InteropUtility.NativeMemoryAllocate((UIntPtr) utf8ByteCount);
        return heapUtf8Pointer;
    }

    public ref struct ManagedToUnmanagedIn {
        private readonly ReadOnlySpan<char> _charSpan;
        private byte* _heapUtf8Pointer;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ManagedToUnmanagedIn(ReadOnlySpan<char> charSpan) {
            _charSpan = charSpan;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ManagedToUnmanagedIn(string str) : this(str.AsSpan()) {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* ConvertToNative(Span<byte> utf8Buffer, out int utf8BytesWritten) {
            // Must be explicitly 0 so pointer arithmetic works out even if we return a null pointer
            utf8BytesWritten = 0;

            ReadOnlySpan<char> utf16 = _charSpan;
            char* utf16Pointer = InteropUtility.SpanToPointer(utf16);
            if (utf16Pointer == null)
                return null;

            int utf8MaxBytes = EstimateNullTerminatedUtf8StringByteCount(utf16.Length);

            // FIXME: disabled as it will break if more than one string method argument needs this
            bool allowUseLongTermBuffer = false;

            byte* utf8Pointer = GetUtf8Buffer(utf8Buffer, utf8MaxBytes, ref _heapUtf8Pointer, allowUseLongTermBuffer);

            // Using utf8MaxByteCount is fine here because by this point
            // it's guaranteed the buffer won't be overrun
            utf8BytesWritten = ConvertUtf16PointerToNullTerminatedUtf8Pointer(
                utf16Pointer,
                utf16.Length,
                utf8Pointer,
                utf8MaxBytes
            );
            return utf8Pointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free() {
            if (_heapUtf8Pointer != null) {
                InteropUtility.NativeMemoryFree((IntPtr) _heapUtf8Pointer);
            }
        }
    }
}

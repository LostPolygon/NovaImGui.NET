using System.Text;

namespace NovaImGuiNet.Interop;

public static unsafe class Utf8StringMarshaller {
    [FixedAddressValueType]
    private static byte EmptyUtf8String; // 0

    private static byte* EmptyUtf8StringPointer;

    #pragma warning disable CA2255
    [ModuleInitializer]
    #pragma warning restore CA2255
    internal static void Initialize() {
        fixed (byte* emptyUtf8StringPtr = &EmptyUtf8String) {
            EmptyUtf8StringPointer = emptyUtf8StringPtr;
        }
    }

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

    public ref struct ManagedToUnmanagedIn {
        public static int StackAllocMaxSize => 1024;

        [ThreadStatic]
        private static NativeMemoryManagedBuffer? ThreadUtf8ConvertBuffer;

        private const int ThreadUtf8ConvertBufferInitialSize = 128 * 1024;
        private const int ThreadUtf8ConvertBufferMaxSize = 4 * ThreadUtf8ConvertBufferInitialSize;
        private const int ThreadUtf8ConvertBufferGrowthChunk = 4096;

        private readonly ReadOnlySpan<char> _charSpan;
        private byte* _utf8Pointer;
        private bool _utf8Allocated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ManagedToUnmanagedIn(ReadOnlySpan<char> charSpan) {
            _charSpan = charSpan;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ManagedToUnmanagedIn(string str) : this(str.AsSpan()) {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* ConvertToNative(Span<byte> utf8Buffer, out int utf8BytesWritten) {
            // Must be explicitly 0 so pointer arithmetic works out even if return a null pointer
            utf8BytesWritten = 0;

            ReadOnlySpan<char> utf16 = _charSpan;
            char* utf16Pointer = InteropUtility.SpanToPointer(utf16);

            if (utf16Pointer == null)
                return null;

            if (utf16.IsEmpty)
                return EmptyUtf8StringPointer;

            int utf8MaxBytes = EstimateNullTerminatedUtf8StringByteCount(utf16.Length);

            bool isStackBufferEnough = utf8MaxBytes <= utf8Buffer.Length;
            if (!isStackBufferEnough) {
                EnsureBuffer(utf8MaxBytes);
            } else {
                _utf8Pointer = InteropUtility.SpanToPointer<byte>(utf8Buffer);
            }

            utf8BytesWritten = ConvertUtf16PointerToNullTerminatedUtf8Pointer(utf16Pointer, utf16.Length, _utf8Pointer, utf8MaxBytes - 1);
            return _utf8Pointer;
        }

        /// <returns>
        /// Non-zero pointer if long-term buffer was resized to the desired size,
        /// zero-pointer if <paramref name="byteCount"/> is larger than the long-term buffer size limit,
        /// and a short-term buffer would need to be allocated instead/
        /// </returns>
        private void EnsureBuffer(int byteCount) {
            // If the requested byte count exceeds the long-term buffer size limit,
            // we'll allocate a short-term buffer that needs to be freed.
            // Otherwise, use the long-term buffer.
            if (byteCount > ThreadUtf8ConvertBufferMaxSize) {
                _utf8Pointer = (byte*) InteropUtility.NativeMemoryAllocate((UIntPtr) byteCount);
                _utf8Allocated = true;
                return;
            }

            // Grow the long-term buffer in chunks
            byteCount = (byteCount / ThreadUtf8ConvertBufferGrowthChunk + 1) * ThreadUtf8ConvertBufferGrowthChunk;
            ThreadUtf8ConvertBuffer ??= new NativeMemoryManagedBuffer((nuint) Math.Max(ThreadUtf8ConvertBufferInitialSize, byteCount));

            _utf8Pointer = (byte*) ThreadUtf8ConvertBuffer.EnsureSize((nuint) byteCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free() {
            if (_utf8Allocated) {
                InteropUtility.NativeMemoryFree((IntPtr) _utf8Pointer);
            }
        }
    }
}

using System.Text;

namespace NovaImGuiNet;

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
    internal static void ConvertUtf16PointerToUtf8Pointer(char* chars, int charCount, byte* bytes, int byteCount) {
        int utf8BytesWritten = Encoding.UTF8.GetBytes(chars, charCount, bytes, byteCount);
        bytes[utf8BytesWritten] = 0;
    }

    public ref struct ManagedToUnmanagedIn {
        public static int StackAllocMaxSize => 1024;

        private readonly ReadOnlySpan<char> _charSpan;
        private readonly bool _isUtf8;
        private readonly char* _utf16Pointer;
        private byte* _utf8Pointer;
        private bool _utf8Allocated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ManagedToUnmanagedIn(ReadOnlySpan<byte> utf8Span) {
            _isUtf8 = true;
            _charSpan = default;

            _utf8Pointer = InteropUtility.SpanToPointer(utf8Span);
            if (_utf8Pointer != null && utf8Span.IsEmpty) {
                _utf8Pointer = EmptyUtf8StringPointer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ManagedToUnmanagedIn(ReadOnlySpan<char> charSpan) {
            _isUtf8 = false;
            _utf8Pointer = null;

            _charSpan = charSpan;
            char* utf16Pointer = InteropUtility.SpanToPointer(charSpan);
            if (utf16Pointer == null) {
                _isUtf8 = true;
            } else if (charSpan.IsEmpty) {
                _isUtf8 = true;
                _utf8Pointer = EmptyUtf8StringPointer;
            } else {
                _utf16Pointer = utf16Pointer;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ManagedToUnmanagedIn(string str) : this(str.AsSpan()) {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte* ConvertToNative(Span<byte> utf8Buffer) {
            if (_isUtf8)
                return _utf8Pointer;

            ReadOnlySpan<char> utf16 = _charSpan;
            int utf8MaxBytes = EstimateNullTerminatedUtf8StringByteCount(utf16.Length);

            bool stackBufferEnough = utf8MaxBytes <= utf8Buffer.Length;
            if (!stackBufferEnough) {
                _utf8Pointer = (byte*) InteropUtility.NativeMemoryAllocate((nuint) utf8MaxBytes);
                _utf8Allocated = true;
            } else {
                _utf8Pointer = InteropUtility.SpanToPointer<byte>(utf8Buffer);
            }

            ConvertUtf16PointerToUtf8Pointer(_utf16Pointer, utf16.Length, _utf8Pointer, utf8MaxBytes - 1);
            return _utf8Pointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free() {
            if (_utf8Allocated) {
                InteropUtility.NativeMemoryFree((IntPtr) _utf8Pointer);
            }
        }
    }
}

#if NETSTANDARD2_0_OR_GREATER
using System.Text;
#endif

namespace NovaImGuiNet;

public readonly unsafe struct NullTerminatedString {
    public readonly byte* Data;

    public NullTerminatedString(byte* data) {
        Data = data;
    }

    public ReadOnlySpan<byte> AsReadOnlyUtf8ByteSpan() {
#if NETSTANDARD2_0_OR_GREATER
        if (Data == null)
            return default;

        int nullByteIndex = new ReadOnlySpan<byte>(Data, Int32.MaxValue).IndexOf((byte) 0);
        return new ReadOnlySpan<byte>(Data, nullByteIndex + 1);
#else
        return MemoryMarshal.CreateReadOnlySpanFromNullTerminated(Data);
#endif
    }

    public override string? ToString() {
#if NET6_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        return Marshal.PtrToStringUTF8((IntPtr) Data);
#else
        if (Data == null)
            return null;

        ReadOnlySpan<byte> utf8ByteSpan = AsReadOnlyUtf8ByteSpan();
        fixed (byte* ptr = &utf8ByteSpan.GetPinnableReference()) {
            return Encoding.UTF8.GetString(ptr, utf8ByteSpan.Length - 1);
        }
#endif
    }

    public static implicit operator string?(NullTerminatedString nts) => nts.ToString();

    public static implicit operator ReadOnlySpan<byte>(NullTerminatedString nts) => nts.AsReadOnlyUtf8ByteSpan();
}

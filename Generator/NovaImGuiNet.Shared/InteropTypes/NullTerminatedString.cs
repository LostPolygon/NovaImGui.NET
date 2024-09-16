#if NETSTANDARD2_0_OR_GREATER
using System.Text;
#endif

namespace NovaImGuiNet;

public readonly unsafe struct NullTerminatedString : IEquatable<NullTerminatedString> {
    public readonly byte* Data;

    public IntPtr DataIntPtr => (IntPtr) Data;

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

    public bool Equals(NullTerminatedString other) => Data == other.Data;

    public override bool Equals(object? obj) => obj is NullTerminatedString other && Equals(other);

    public override int GetHashCode() => unchecked((int) (long) Data);

    public static bool operator ==(NullTerminatedString left, NullTerminatedString right) => left.Equals(right);

    public static bool operator !=(NullTerminatedString left, NullTerminatedString right) => !left.Equals(right);

    public static implicit operator string?(NullTerminatedString nts) => nts.ToString();

    public static implicit operator ReadOnlySpan<byte>(NullTerminatedString nts) => nts.AsReadOnlyUtf8ByteSpan();

    public static implicit operator byte*(NullTerminatedString nts) => nts.Data;

    public static implicit operator IntPtr(NullTerminatedString nts) => (IntPtr) nts.Data;
}

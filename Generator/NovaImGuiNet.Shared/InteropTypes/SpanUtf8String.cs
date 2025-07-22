using System.Text;

namespace NovaImGuiNet;

// FIXME: account for null termination in fixed length buffers
public readonly ref struct SpanUtf8String {
    public readonly Span<byte> Data;

    public SpanUtf8String(Span<byte> data) => Data = data;

    public ReadOnlySpan<byte> AsReadOnlyUtf8ByteSpan() => Data;

    public override string? ToString() => null;
    //public override string? ToString() => Encoding.UTF8.GetString(Data);

    public bool Equals(SpanUtf8String other) => Data == other.Data;

    public override int GetHashCode() => Data.GetHashCode();

    public static implicit operator string?(SpanUtf8String nts) => nts.ToString();

    public static implicit operator ReadOnlySpan<byte>(SpanUtf8String nts) => nts.AsReadOnlyUtf8ByteSpan();
}

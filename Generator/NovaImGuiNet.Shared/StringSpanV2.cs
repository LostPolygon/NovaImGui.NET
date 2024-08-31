using System.Runtime.CompilerServices;

namespace NovaImGuiNet;

/// <summary>
/// Represents a string of either UTF-8 or UTF-16 characters.
/// </summary>
public readonly unsafe ref struct StringSpanV2 {
    public readonly bool IsUtf8Span;

    public readonly ReadOnlySpan<byte> Utf8Span;
    public readonly ReadOnlySpan<char> CharSpan;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringSpanV2(ReadOnlySpan<byte> utf8Span) {
        IsUtf8Span = true;
        Utf8Span = utf8Span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringSpanV2(ReadOnlySpan<char> charSpan) {
        IsUtf8Span = false;
        CharSpan = charSpan;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StringSpanV2(string? str) => new(str.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StringSpanV2(Span<byte> span) => new(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StringSpanV2(ReadOnlySpan<byte> span) => new(span);
}

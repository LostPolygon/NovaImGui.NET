using System.Runtime.CompilerServices;

namespace NovaImGuiNet;

/// <summary>
/// Represents a string of either UTF-8 or UTF-16 characters.
/// </summary>
public readonly unsafe ref struct StringSpan {
    public readonly bool IsUtf8;

    //public readonly ReadOnlySpan<byte> Utf8Span;
    public readonly ReadOnlySpan<char> CharSpan;
    public readonly byte* Utf8Pointer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringSpan(ReadOnlySpan<byte> utf8Span) {
        IsUtf8 = true;
        CharSpan = default;

        //Utf8Span = utf8Span;
        Utf8Pointer = InteropUtility.SpanToPointer(utf8Span);
        if (Utf8Pointer != null && utf8Span.IsEmpty) {
            Utf8Pointer = InteropUtility.EmptyUtf8StringPointer;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StringSpan(ReadOnlySpan<char> charSpan) {
        IsUtf8 = false;
        Utf8Pointer = null;

        CharSpan = charSpan;
        char* utf16Pointer = InteropUtility.SpanToPointer(charSpan);
        if (utf16Pointer == null) {
            IsUtf8 = true;
        } else if (charSpan.IsEmpty) {
            IsUtf8 = true;
            Utf8Pointer = InteropUtility.EmptyUtf8StringPointer;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StringSpan(string? str) => new(str.AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StringSpan(Span<byte> span) => new(span);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator StringSpan(ReadOnlySpan<byte> span) => new(span);
}

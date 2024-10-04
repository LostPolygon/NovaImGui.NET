using Utf8StringInterpolation;

namespace NovaImGuiNet.Interop;

[InterpolatedStringHandler]
public unsafe ref struct ImGuiUtf8StringWriter {
    public readonly int LiteralLength;
    public readonly int FormattedCount;

    public ReadOnlySpan<byte> WrittenSpan;
    internal Utf8StringWriter<OptimizedBufferWriter<byte>> Utf8StringWriter;

    private fixed byte _stackBuffer[Utf8StringMarshaller.StackAllocMaxSize];
    private int _literalLengthCurrent;
    private int _formattedCountCurrent;

    public bool IsCompleted => LiteralLength == _literalLengthCurrent && FormattedCount == _formattedCountCurrent;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImGuiUtf8StringWriter(int literalLength, int formattedCount, IFormatProvider? formatProvider = null) {
        LiteralLength = literalLength;
        FormattedCount = formattedCount;
        Utf8StringWriter = new Utf8StringWriter<OptimizedBufferWriter<byte>>(
            literalLength,
            formattedCount,
            new OptimizedBufferWriter<byte>(
                (byte*) Unsafe.AsPointer(ref _stackBuffer[0]),
                Utf8StringMarshaller.StackAllocMaxSize
            ),
            formatProvider
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free() {
        Utf8StringWriter.GetBufferWriter().Free();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Flush() {
        Utf8StringWriter.Flush();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal OptimizedBufferWriter<byte> GetBufferWriter() {
        return Utf8StringWriter.GetBufferWriter();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetFinalSpan() {
        return Utf8StringWriter.GetBufferWriter().WrittenSpan;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, int alignment = 0, string? format = null) {
        Utf8StringWriter.AppendFormatted(value, alignment, format);
        _formattedCountCurrent++;
        CheckIsCompletedAndFinalize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string s) {
        Utf8StringWriter.AppendFormatted(s.AsSpan());
        _literalLengthCurrent += s.Length;
        CheckIsCompletedAndFinalize();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckIsCompletedAndFinalize() {
        if (!IsCompleted)
            return;

        // TODO: don't add zero-terminator if allowed
        Utf8StringWriter.AppendUtf8("\0"u8);
        Utf8StringWriter.Flush();
        WrittenSpan = Utf8StringWriter.GetBufferWriter().WrittenSpan;
    }
}

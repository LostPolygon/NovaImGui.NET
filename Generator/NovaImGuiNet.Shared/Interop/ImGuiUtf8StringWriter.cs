using Utf8StringInterpolation;

namespace NovaImGuiNet.Interop;

[InterpolatedStringHandler]
public unsafe ref struct ImGuiUtf8StringWriter {
    private Utf8StringWriter<OptimizedBufferWriter<byte>> _utf8StringWriter;
    private fixed byte _stackBuffer[Utf8StringMarshaller.StackAllocMaxSize];
    private bool _hasFinalWrittenSpan;
    private ReadOnlySpan<byte> _finalWrittenSpan;
    private bool _isFinalWrittenSpanNullTerminated;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImGuiUtf8StringWriter(int literalLength, int formattedCount, IFormatProvider? formatProvider = null) {
        _utf8StringWriter = new Utf8StringWriter<OptimizedBufferWriter<byte>>(
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
        _utf8StringWriter.GetBufferWriter().Free();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Flush() {
        _utf8StringWriter.Flush();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal OptimizedBufferWriter<byte> GetBufferWriter() {
        return _utf8StringWriter.GetBufferWriter();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetFinalWrittenSpan() {
        if (_hasFinalWrittenSpan)
            return _finalWrittenSpan;

        return GetFinalWrittenSpanCore(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> GetFinalNullTerminatedWrittenSpan() {
        if (_hasFinalWrittenSpan) {
            return _finalWrittenSpan;
        }

        _utf8StringWriter.AppendUtf8("\0"u8);
        return GetFinalWrittenSpanCore(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendFormatted<T>(T value, int alignment = 0, string? format = null) {
        _utf8StringWriter.AppendFormatted(value, alignment, format);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLiteral(string s) {
        _utf8StringWriter.AppendFormatted(s.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> GetFinalWrittenSpanCore(bool nullTerminated) {
        _utf8StringWriter.Flush();
        ReadOnlySpan<byte> finalWrittenSpan = _utf8StringWriter.GetBufferWriter().WrittenSpan;
        _finalWrittenSpan = finalWrittenSpan;
        _hasFinalWrittenSpan = true;
        _isFinalWrittenSpanNullTerminated = nullTerminated;
        return finalWrittenSpan;
    }

    // Unsafe.AsRef doesn't work with ref structs because generics don't support those
    // Instead, emit our own non-generic version
    // https://github.com/dotnet/runtime/issues/68709
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref ImGuiUtf8StringWriter UnsafeAsRef(in ImGuiUtf8StringWriter source) {
        InlineIL.IL.Emit.Ldarg_0();
        InlineIL.IL.Emit.Ret();
        throw InlineIL.IL.Unreachable();
    }
}

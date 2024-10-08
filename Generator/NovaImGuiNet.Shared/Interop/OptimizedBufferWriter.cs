using System.Buffers;
using System.Diagnostics;

namespace NovaImGuiNet.Interop;

#if !NET6_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
using ABWNS = NovaImGuiNet.Interop;
#else
using ABWNS = System.Buffers;
#endif

/// <summary>
/// Uses a small buffer (presumably stackalloc-ed) before switching to an ArrayBufferWriter.
/// </summary>
/// <typeparam name="T"></typeparam>
internal unsafe struct OptimizedBufferWriter<T> : IBufferWriter<T> where T : unmanaged {
    private readonly byte* _buffer;
    private readonly int _bufferSize;
    private int _index;
    private ABWNS::ArrayBufferWriter<T>? _arrayBufferWriter;

    /// <summary>
    /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _arrayBufferWriter != null ? _arrayBufferWriter.WrittenSpan : new(_buffer, _index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OptimizedBufferWriter(byte* buffer, int bufferSize) {
        _buffer = buffer;
        _bufferSize = bufferSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) {
        if (_arrayBufferWriter != null) {
            _arrayBufferWriter.Advance(count);
            return;
        }

        Debug.Assert(count >= 0, "count >= 0");
        Debug.Assert(_index <= _bufferSize - count, "_index > _bufferSize - count");

        _index += count;
    }

    public Memory<T> GetMemory(int sizeHint = 0) {
        throw new NotSupportedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan(int sizeHint = 0) {
        if (_arrayBufferWriter != null)
            return _arrayBufferWriter.GetSpan(sizeHint);

        if (sizeHint > _bufferSize) {
            return SwitchToArrayBufferWriter(sizeHint);
        }

        return new Span<T>(_buffer + _index, Utf8StringMarshaller.StackAllocMaxSize - _index);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Span<T> SwitchToArrayBufferWriter(int sizeHint) {
        ArrayBufferWriter<T> arrayBufferWriter = ArrayBufferWriterPool<T>.Rent(sizeHint);
        _arrayBufferWriter = arrayBufferWriter;

        Span<T> result = arrayBufferWriter.GetSpan(sizeHint);

        // Copy from existing buffer to ArrayBufferWriter
        if (_index != 0) {
            arrayBufferWriter.Advance(_index);
            new ReadOnlySpan<T>(_buffer, _index).CopyTo(result);
            return result[_index..];
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free() {
        if (_arrayBufferWriter != null) {
            ArrayBufferWriterPool<T>.Return(_arrayBufferWriter);
        }
    }
}

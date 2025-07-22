using System.Buffers;
using System.Diagnostics;

namespace NovaImGuiNet.Interop;

/// <summary>
/// Uses a small provided buffer (presumably stackalloc-ed) before switching to an NativeBufferWriter.
/// </summary>
/// <typeparam name="T"></typeparam>
internal unsafe struct OptimizedBufferWriter<T> : IBufferWriter<T> where T : unmanaged {
    private readonly byte* _buffer;
    private readonly int _bufferSize;
    private int _index;
    private NativeMemoryBufferWriter<T>? _nativeMemoryBufferWriter;

    /// <summary>
    /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _nativeMemoryBufferWriter != null ? _nativeMemoryBufferWriter.WrittenSpan : new(_buffer, _index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OptimizedBufferWriter(byte* buffer, int bufferSize) {
        _buffer = buffer;
        _bufferSize = bufferSize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) {
        if (_nativeMemoryBufferWriter != null) {
            _nativeMemoryBufferWriter.Advance(count);
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
        if (_nativeMemoryBufferWriter != null)
            return _nativeMemoryBufferWriter.GetSpan(sizeHint);

        if (sizeHint > _bufferSize) {
            return SwitchToNativeBufferWriter(sizeHint);
        }

        return new Span<T>(_buffer + _index, Utf8StringMarshaller.StackAllocMaxSize - _index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Free() {
        if (_nativeMemoryBufferWriter != null) {
            NativeMemoryBufferWriterPool<T>.Return(_nativeMemoryBufferWriter);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Span<T> SwitchToNativeBufferWriter(int sizeHint) {
        NativeMemoryBufferWriter<T> nativeMemoryBufferWriter = NativeMemoryBufferWriterPool<T>.Rent(sizeHint);
        _nativeMemoryBufferWriter = nativeMemoryBufferWriter;

        Span<T> result = nativeMemoryBufferWriter.GetSpan(sizeHint);

        // Copy from existing buffer to NativeMemoryBufferWriter
        if (_index != 0) {
            nativeMemoryBufferWriter.Advance(_index);
            new ReadOnlySpan<T>(_buffer, _index).CopyTo(result);
            return result[_index..];
        }

        return result;
    }
}

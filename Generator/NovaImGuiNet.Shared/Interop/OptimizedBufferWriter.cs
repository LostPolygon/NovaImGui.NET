using System.Buffers;
using System.Diagnostics;

namespace NovaImGuiNet.Interop;

/// <summary>
/// Uses a small buffer (presumably stackalloc-ed) before switching to an ArrayBufferWriter.
/// </summary>
/// <typeparam name="T"></typeparam>
internal unsafe struct OptimizedBufferWriter<T> : IBufferWriter<T> where T : unmanaged {
    private readonly byte* _buffer;
    private readonly int _bufferSize;
    private int _index;
    private ArrayBufferWriter<T>? _arrayBufferWriter;

    /// <summary>
    /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan =>
        _arrayBufferWriter != null ?
            _arrayBufferWriter.WrittenSpan :
            new(_buffer, _index);

    public OptimizedBufferWriter(byte* buffer, int bufferSize) {
        _buffer = buffer;
        _bufferSize = bufferSize;
    }

    public void Advance(int count) {
        if (_arrayBufferWriter != null) {
            _arrayBufferWriter.Advance(count);
            return;
        }

        Debug.Assert(count >= 0, "count >= 0");
        Debug.Assert(_index <= _bufferSize - count, $"_index > _bufferSize - count, buffer size is {_bufferSize}");

        _index += count;
    }

    public Memory<T> GetMemory(int sizeHint = 0) {
        throw new NotSupportedException();
    }

    public Span<T> GetSpan(int sizeHint = 0) {
        if (_arrayBufferWriter != null)
            return _arrayBufferWriter.GetSpan(sizeHint);

        // Switch to ArrayBufferWriter
        if (sizeHint > _bufferSize) {
            ArrayBufferWriter<T> arrayBufferWriter = ArrayBufferWriterPool<T>.Rent(sizeHint);
            _arrayBufferWriter = arrayBufferWriter;

            Span<T> result = arrayBufferWriter.GetSpan(sizeHint);

            // Copy from provided buffer to ArrayBufferWriter
            if (_index != 0) {
                arrayBufferWriter.Advance(_index);
                new Span<T>(_buffer, _index).CopyTo(result);
                return result[_index..];
            }

            return result;
        }

        return new Span<T>(_buffer + _index, Utf8StringMarshaller.StackAllocMaxSize - _index);
    }

    public void Free() {
        if (_arrayBufferWriter != null) {
            ArrayBufferWriterPool<T>.Return(_arrayBufferWriter);
        }
    }
}

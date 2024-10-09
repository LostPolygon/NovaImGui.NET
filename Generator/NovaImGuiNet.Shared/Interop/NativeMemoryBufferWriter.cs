#define DISABLE_SAFETY
using System.Buffers;
using System.Diagnostics;

namespace NovaImGuiNet.Interop;

/// <summary>
/// Represents a heap-based, array-backed output sink into which <typeparamref name="T"/> data can be written.
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed unsafe class NativeMemoryBufferWriter<T> : IBufferWriter<T>, IDisposable {
#if DEBUG
    // Copy of Array.MaxLength.
    // Used by projects targeting .NET Framework.
    private const int ArrayMaxLength = 0x7FFFFFC7;

    private const int DefaultInitialBufferSize = 256;
#endif

    private IntPtr _buffer;
    private int _bufferSizeBytes;
    private int _index;

    /// <summary>
    /// Creates an instance of an <see cref="NativeMemoryBufferWriter{T}"/>, in which data can be written to,
    /// with an initial capacity specified.
    /// </summary>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="initialCapacity"/> is not positive (i.e. less than or equal to 0).
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeMemoryBufferWriter(int initialCapacity) {
#if !DISABLE_SAFETY
        if (initialCapacity <= 0)
            throw new ArgumentException(null, nameof(initialCapacity));
#endif

        _buffer = InteropUtility.NativeMemoryAllocate((nuint) initialCapacity);
        _bufferSizeBytes = initialCapacity;
        _index = 0;
    }

    public IntPtr Buffer => _buffer;

    public int BufferSizeBytes => _bufferSizeBytes;

    /// <summary>
    /// Returns the data written to the underlying buffer so far, as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new Span<T>((void*) _buffer, _index);
    }

    /// <summary>
    /// Returns the amount of data written to the underlying buffer so far.
    /// </summary>
    public int WrittenCount => _index;

    /// <summary>
    /// Returns the total amount of space within the underlying buffer.
    /// </summary>
    public int Capacity => _bufferSizeBytes;

    /// <summary>
    /// Returns the amount of space available that can still be written into without forcing the underlying buffer to grow.
    /// </summary>
    public int FreeCapacity {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _bufferSizeBytes - _index * Marshal.SizeOf<T>();
    }

    /// <summary>
    /// Clears the data written to the underlying buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must reset or clear the <see cref="NativeMemoryBufferWriter{T}"/> before trying to re-use it.
    /// </para>
    /// <para>
    /// The <see cref="ResetWrittenCount"/> method is faster since it only sets to zero the writer's index
    /// while the <see cref="Clear"/> method additionally zeroes the content of the underlying buffer.
    /// </para>
    /// </remarks>
    /// <seealso cref="ResetWrittenCount"/>
    public void Clear() {
        Debug.Assert(_bufferSizeBytes >= _index);
        new Span<T>((void*) _buffer, _index).Clear();
        _index = 0;
    }

    /// <summary>
    /// Resets the data written to the underlying buffer without zeroing its content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You must reset or clear the <see cref="NativeMemoryBufferWriter{T}"/> before trying to re-use it.
    /// </para>
    /// <para>
    /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, the underlying buffer will not be cleared.
    /// </para>
    /// </remarks>
    /// <seealso cref="Clear"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetWrittenCount() => _index = 0;

    /// <summary>
    /// Notifies <see cref="IBufferWriter{T}"/> that <paramref name="count"/> amount of data was written to the output <see cref="Span{T}"/>/<see cref="Memory{T}"/>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="count"/> is negative.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to advance past the end of the underlying buffer.
    /// </exception>
    /// <remarks>
    /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) {
#if !DISABLE_SAFETY
        if (count < 0)
            throw new ArgumentException(null, nameof(count));

        if (_index > _bufferSizeBytes - count)
            ThrowInvalidOperationException_AdvancedTooFar(_bufferSizeBytes);
#endif

        _index += count;
    }

    /// <summary>
    /// Returns a <see cref="Memory{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
    /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sizeHint"/> is negative.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This will never return an empty <see cref="Memory{T}"/>.
    /// </para>
    /// <para>
    /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
    /// </para>
    /// <para>
    /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
    /// </para>
    /// <para>
    /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, this method may return a non-cleared <see cref="Memory{T}"/>.
    /// </para>
    /// <para>
    /// If you clear the writer using the <see cref="Clear"/> method, this method will return a <see cref="Memory{T}"/> with its content zeroed.
    /// </para>
    /// </remarks>
    public Memory<T> GetMemory(int sizeHint = 0) {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Returns a <see cref="Span{T}"/> to write to that is at least the requested length (specified by <paramref name="sizeHint"/>).
    /// If no <paramref name="sizeHint"/> is provided (or it's equal to <code>0</code>), some non-empty buffer is returned.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sizeHint"/> is negative.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This will never return an empty <see cref="Span{T}"/>.
    /// </para>
    /// <para>
    /// There is no guarantee that successive calls will return the same buffer or the same-sized buffer.
    /// </para>
    /// <para>
    /// You must request a new buffer after calling Advance to continue writing more data and cannot write to a previously acquired buffer.
    /// </para>
    /// <para>
    /// If you reset the writer using the <see cref="ResetWrittenCount"/> method, this method may return a non-cleared <see cref="Span{T}"/>.
    /// </para>
    /// <para>
    /// If you clear the writer using the <see cref="Clear"/> method, this method will return a <see cref="Span{T}"/> with its content zeroed.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan(int sizeHint = 0) {
        CheckAndResizeBuffer(sizeHint);
        Debug.Assert(_bufferSizeBytes > _index);
        return new Span<T>((void*) _buffer, _bufferSizeBytes).Slice(_index * Marshal.SizeOf<T>());
    }

    private void CheckAndResizeBuffer(int sizeHint) {
#if !DISABLE_SAFETY
        if (sizeHint < 0)
            throw new ArgumentException(nameof(sizeHint));

        if (sizeHint == 0) {
            sizeHint = 1;
        }
#endif

        if (sizeHint > FreeCapacity) {
            int currentLength = _bufferSizeBytes;

            // Attempt to grow by the larger of the sizeHint and double the current size.
            int growBy = Math.Max(sizeHint, currentLength);

#if !DISABLE_SAFETY
            if (currentLength == 0) {
                growBy = Math.Max(growBy, DefaultInitialBufferSize);
            }
#endif

            int newSize = currentLength + growBy;

#if DEBUG
            if ((uint) newSize > int.MaxValue) {
                // Attempt to grow to ArrayMaxLength.
                uint needed = (uint) (currentLength - FreeCapacity + sizeHint);
                Debug.Assert(needed > currentLength);

                if (needed > ArrayMaxLength) {
                    ThrowOutOfMemoryException(needed);
                }

                newSize = ArrayMaxLength;
            }
#endif

            _buffer = InteropUtility.NativeMemoryReallocate(_buffer, (nuint) newSize);
            _bufferSizeBytes = newSize;
        }

        Debug.Assert(FreeCapacity > 0 && FreeCapacity >= sizeHint);
    }

    private void ReleaseUnmanagedResources() {
        if (_buffer != IntPtr.Zero) {
            InteropUtility.NativeMemoryFree(_buffer);
            _buffer = IntPtr.Zero;
        }
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~NativeMemoryBufferWriter() {
        ReleaseUnmanagedResources();
    }

    private static void ThrowInvalidOperationException_AdvancedTooFar(int capacity) {
        throw new InvalidOperationException();
    }

    private static void ThrowOutOfMemoryException(uint capacity) {
        throw new OutOfMemoryException();
    }
}

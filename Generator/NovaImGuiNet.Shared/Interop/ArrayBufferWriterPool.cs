using System.Buffers;
using System.Collections.Concurrent;

namespace NovaImGuiNet.Interop;

internal static class ArrayBufferWriterPool<T> {
    private static readonly ConcurrentQueue<ArrayBufferWriter<T>> Cache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayBufferWriter<T> Rent(int initialCapacityHint) {
        if (Cache.TryDequeue(out ArrayBufferWriter<T>? writer)) {
            return writer;
        }

        return new ArrayBufferWriter<T>(initialCapacityHint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(ArrayBufferWriter<T> writer) {
#if NET8_0_OR_GREATER
        writer.ResetWrittenCount();
#else
        writer.Clear();
#endif
        Cache.Enqueue(writer);
    }
}

using System.Collections.Concurrent;

namespace NovaImGuiNet.Interop;

#if !NET6_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
using abfns = NovaImGuiNet.Interop;
#else
using abfns = System.Buffers;
#endif

internal static class ArrayBufferWriterPool<T> {
    private static readonly ConcurrentQueue<abfns::ArrayBufferWriter<T>> Cache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static abfns::ArrayBufferWriter<T> Rent(int initialCapacityHint) {
        if (Cache.TryDequeue(out abfns::ArrayBufferWriter<T>? writer)) {
            return writer;
        }

        return new abfns::ArrayBufferWriter<T>(initialCapacityHint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(abfns::ArrayBufferWriter<T> writer) {
#if NET8_0_OR_GREATER
        writer.ResetWrittenCount();
#else
        writer.Clear();
#endif
        Cache.Enqueue(writer);
    }
}

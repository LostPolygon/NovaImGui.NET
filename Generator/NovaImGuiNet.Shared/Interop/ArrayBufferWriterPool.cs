using System.Collections.Concurrent;

namespace NovaImGuiNet.Interop;

#if !NET6_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER
using ABWNS = NovaImGuiNet.Interop;
#else
using ABWNS = System.Buffers;
#endif

internal static class ArrayBufferWriterPool<T> {
    private static readonly ConcurrentQueue<ABWNS::ArrayBufferWriter<T>> Cache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ABWNS::ArrayBufferWriter<T> Rent(int initialCapacityHint) {
        if (Cache.TryDequeue(out ABWNS::ArrayBufferWriter<T>? writer)) {
            return writer;
        }

        return new ABWNS::ArrayBufferWriter<T>(initialCapacityHint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(ABWNS::ArrayBufferWriter<T> writer) {
#if NET8_0_OR_GREATER
        writer.ResetWrittenCount();
#else
        writer.Clear();
#endif
        Cache.Enqueue(writer);
    }
}

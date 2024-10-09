namespace NovaImGuiNet.Interop;

internal static class NativeMemoryBufferWriterPool<T> {
    [ThreadStatic]
    private static Queue<NativeMemoryBufferWriter<T>>? Cache;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeMemoryBufferWriter<T> Rent(int initialCapacityHint) {
#if NETSTANDARD2_0
        Queue<NativeMemoryBufferWriter<T>> cache = GetCache();
        if (cache.Count != 0)
            return cache.Dequeue();
#else
        if (GetCache().TryDequeue(out NativeMemoryBufferWriter<T>? writer))
            return writer;
#endif

        return new NativeMemoryBufferWriter<T>(initialCapacityHint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(NativeMemoryBufferWriter<T> writer) {
        writer.ResetWrittenCount();
        GetCache().Enqueue(writer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Queue<NativeMemoryBufferWriter<T>> GetCache() =>
        Cache ??= new Queue<NativeMemoryBufferWriter<T>>();
}

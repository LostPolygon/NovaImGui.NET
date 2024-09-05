namespace NovaImGuiNet;

public record struct ImChunkStream<T> where T : unmanaged {
    public ImVector<byte> Buf;
}

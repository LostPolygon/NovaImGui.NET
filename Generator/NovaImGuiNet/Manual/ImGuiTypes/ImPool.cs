namespace NovaImGuiNet;

public record struct ImPool<T> where T : unmanaged, IEquatable<T> {
    ///<summary>Contiguous data</summary>
    public ImVector<T> Buf;
    ///<summary>ID-&gt;Index</summary>
    public ImGuiStorage Map;
    ///<summary>Next free idx to use</summary>
    public ImPoolIdx FreeIdx;
    ///<summary>Number of active/alive items (for display purpose)</summary>
    public ImPoolIdx AliveCount;
}

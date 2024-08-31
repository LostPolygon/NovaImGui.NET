namespace NovaImGuiNet;

public struct ImPool<T> where T : unmanaged {
    ///<summary>Contiguous data</summary>
    public ImVector<T> Buf;
    ///<summary>ID-&gt;Index</summary>
    public ImGuiStorage Map;
    ///<summary>Next free idx to use</summary>
    public ImPoolIdx FreeIdx;
    ///<summary>Number of active/alive items (for display purpose)</summary>
    public ImPoolIdx AliveCount;
}

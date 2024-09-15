#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    ///<summary>modify a style color. always use this if you modify the style after NewFrame().</summary>
    public static void PushStyleColor(ImGuiCol idx, ImColor col) => PushStyleColor(idx, col.Value);
}

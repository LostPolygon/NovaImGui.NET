namespace NovaImGuiNet;

public static partial class ImGuiConstants {
    // #if IMGUI_USE_BGRA_PACKED_COLOR
    public const uint IM_COL32_WHITE = 0xFFFFFFFF;
    public const uint IM_COL32_BLACK = 0x000000FF;
    public const uint IM_COL32_BLACK_TRANS = 0x00000000;
    public const uint IM_COL32_RED = unchecked((uint) (0xFF << IM_COL32_A_SHIFT | 0x00 << IM_COL32_B_SHIFT | 0x00 << IM_COL32_G_SHIFT | 0xFF << IM_COL32_R_SHIFT));
    public const float FLT_MAX = Single.MaxValue;
    public const float FLT_MIN = Single.MinValue;
}

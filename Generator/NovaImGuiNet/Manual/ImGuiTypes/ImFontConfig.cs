namespace NovaImGuiNet;

public unsafe partial struct ImFontConfig {
    public ImFontConfig() {
        this = Create();
    }

    public static ImFontConfig Create() {
        ImFontConfig config = default;
        config.FontDataOwnedByAtlas = true;
        config.FontNo = 0;
        config.OversampleH = 2;
        config.OversampleV = 1;
        config.PixelSnapH = false;
        config.GlyphExtraSpacing = new ImVec2(0, 0);
        config.GlyphOffset = new ImVec2(0, 0);
        config.GlyphRanges = null;
        config.GlyphMinAdvanceX = 0;
        config.GlyphMaxAdvanceX = FLT_MAX;
        config.MergeMode = false;
        config.FontBuilderFlags = 0;
        config.RasterizerMultiply = 1f;
        config.RasterizerDensity = 1f;
        config.EllipsisChar = unchecked((ImWchar) (-1));
        return config;
    }
}

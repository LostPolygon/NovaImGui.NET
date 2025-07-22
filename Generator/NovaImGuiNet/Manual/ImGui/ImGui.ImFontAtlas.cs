namespace NovaImGuiNet;

public partial struct ImFontAtlas {
#if !IMGUI_DISABLE_OBSOLETE_FUNCTIONS
    public ImTextureRef TexRef => TexRef_.TexRef;
    public ImTextureRef TexId => TexRef_.TexID;
#else
#endif
}

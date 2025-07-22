namespace NovaImGuiNet;

public partial struct ImTextureRef {
    public static implicit operator ImTextureRef(ImTextureID texId) => new ImTextureRef(texId);
}

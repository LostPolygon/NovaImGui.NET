namespace NovaImGuiNet.ImGuiInternal;

public static class ImGuiViewportExtensions {
    public static unsafe ref ImGuiViewportP AsImGuiViewportP(this ref ImGuiViewport viewport) {
        return ref Unsafe.AsRef<ImGuiViewportP>(Unsafe.AsPointer(ref viewport));
    }
}

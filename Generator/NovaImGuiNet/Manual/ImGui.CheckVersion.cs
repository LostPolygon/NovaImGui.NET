namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    public static void CheckVersion() {
        bool isAbiValid = ImGui.DebugCheckVersionAndDataLayout(
            IMGUI_VERSION.AsSpan(),
            sizeof(ImGuiIO),
            sizeof(ImGuiStyle),
            sizeof(ImVec2),
            sizeof(ImVec4),
            sizeof(ImDrawVert),
            sizeof(ImDrawIdx)
        );

        if (!isAbiValid) {
            string errorMessage =
                $"""
                 Native cimgui library ABI doesn't match the managed assembly ABI.
                 Please check if you are using matching versions of NovaImGuiNet and NovaImGuiNet.Native packages.

                 Mismatch report:

                 Operating System: {Environment.OSVersion}
                 NovaImGuiNet version: {ThisAssembly.AssemblyInformationalVersion}
                 Dear ImGui version: {new NullTerminatedString(ImGui.GetVersion())}
                 Expected Dear ImGui version: {IMGUI_VERSION}

                 sizeof(IntPtr): {sizeof(IntPtr)}
                 sizeof(ImGuiIO): {sizeof(ImGuiIO)}
                 sizeof(ImGuiStyle): {sizeof(ImGuiStyle)}
                 sizeof(ImVec2): {sizeof(ImVec2)}
                 sizeof(ImVec4): {sizeof(ImVec4)}
                 sizeof(ImDrawVert): {sizeof(ImDrawVert)}
                 sizeof(ImDrawIdx): {sizeof(ImDrawIdx)}
                 """;

            throw new Exception(errorMessage);
        }
    }
}

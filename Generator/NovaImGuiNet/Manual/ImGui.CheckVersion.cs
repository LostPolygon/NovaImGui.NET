#nullable enable
namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    /// <summary>
    /// Check that version and structures layouts are matching between native Dear ImGui code and the .NET assembly.
    /// </summary>
    public static void CheckVersion() {
        bool isAbiValid = DebugCheckVersionAndDataLayout(
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
                 Expected Dear ImGui version: {IMGUI_VERSION}
                 Actual Dear ImGui version: {GetVersion()}

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

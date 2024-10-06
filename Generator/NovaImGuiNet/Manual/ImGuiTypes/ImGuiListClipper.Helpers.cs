#nullable enable

namespace NovaImGuiNet;

public partial struct ImGuiListClipper : IDisposable {
    public void Dispose() {
        End();
    }
}

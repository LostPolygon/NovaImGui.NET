#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    ///<summary>return true when activated.</summary>
    public static bool MenuItem(ReadOnlySpan<byte> label, bool selected, bool enabled = true)
        => MenuItem(label, (ReadOnlySpan<byte>) default, selected, enabled);

    ///<summary>return true when activated.</summary>
    public static bool MenuItem(ReadOnlySpan<char> label, bool selected, bool enabled = true)
        => MenuItem(label, (ReadOnlySpan<byte>) default, selected, enabled);

#if NETSTANDARD2_0
    ///<summary>return true when activated.</summary>
    public static bool MenuItem(string label, bool selected, bool enabled = true)
        => MenuItem(label, (ReadOnlySpan<byte>) default, selected, enabled);
#endif

    ///<summary>return true when activated.</summary>
    public static bool MenuItem(ReadOnlySpan<byte> label, ref bool selected, bool enabled = true)
        => MenuItem(label, (ReadOnlySpan<byte>) default, ref selected, enabled);

    ///<summary>return true when activated.</summary>
    public static bool MenuItem(ReadOnlySpan<char> label, ref bool selected, bool enabled = true)
        => MenuItem(label, (ReadOnlySpan<byte>) default, ref selected, enabled);

#if NETSTANDARD2_0
    ///<summary>return true when activated.</summary>
    public static bool MenuItem(string label, ref bool selected, bool enabled = true)
        => MenuItem(label, (ReadOnlySpan<byte>) default, ref selected, enabled);
#endif
}

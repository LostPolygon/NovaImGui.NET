#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    ///<summary>
    ///  Inputs Utilities: Shortcut Testing &amp; Routing [BETA]
    ///  - ImGuiKeyChord = a ImGuiKey + optional ImGuiMod_Alt/ImGuiMod_Ctrl/ImGuiMod_Shift/ImGuiMod_Super.
    ///  ImGuiKey_C                          // Accepted by functions taking ImGuiKey or ImGuiKeyChord arguments)
    ///  ImGuiMod_Ctrl | ImGuiKey_C          // Accepted by functions taking ImGuiKeyChord arguments)
    ///  only ImGuiMod_XXX values are legal to combine with an ImGuiKey. You CANNOT combine two ImGuiKey values.
    ///  - The general idea is that several callers may register interest in a shortcut, and only one owner gets it.
    ///  Parent   -&gt; call Shortcut(Ctrl+S)    // When Parent is focused, Parent gets the shortcut.
    ///  Child1 -&gt; call Shortcut(Ctrl+S)    // When Child1 is focused, Child1 gets the shortcut (Child1 overrides Parent shortcuts)
    ///  Child2 -&gt; no call                  // When Child2 is focused, Parent gets the shortcut.
    ///  The whole system is order independent, so if Child1 makes its calls before Parent, results will be identical.
    ///  This is an important property as it facilitate working with foreign code or larger codebase.
    ///  - To understand the difference:
    ///  - IsKeyChordPressed() compares mods and call IsKeyPressed() -&gt; function has no side-effect.
    ///  - Shortcut() submits a route, routes are resolved, if it currently can be routed it calls IsKeyChordPressed() -&gt; function has (desirable) side-effects as it can prevents another call from getting the route.
    ///  - Visualize registered routes in &#39;Metrics/Debugger-&gt;Inputs&#39;.
    ///</summary>
    public static bool Shortcut(ImGuiKey keyChord, ImGuiInputFlags flags = ImGuiInputFlags.None) =>
        ImGuiNative.ImGui_Shortcut((ImGuiKeyChord) keyChord, flags);
}

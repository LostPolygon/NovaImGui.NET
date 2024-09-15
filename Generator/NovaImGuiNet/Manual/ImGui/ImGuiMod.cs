namespace NovaImGuiNet;

[Flags]
public enum ImGuiMod {
    ///<summary>
    ///  Keyboard Modifiers (explicitly submitted by backend via AddKeyEvent() calls)
    ///  - This is mirroring the data also written to io.KeyCtrl, io.KeyShift, io.KeyAlt, io.KeySuper, in a format allowing
    ///  them to be accessed via standard key API, allowing calls such as IsKeyPressed(), IsKeyReleased(), querying duration etc.
    ///  - Code polling every key (e.g. an interface to detect a key press for input mapping) might want to ignore those
    ///  and prefer using the real keys (e.g. ImGuiKey_LeftCtrl, ImGuiKey_RightCtrl instead of ImGuiMod_Ctrl).
    ///  - In theory the value of keyboard modifiers should be roughly equivalent to a logical or of the equivalent left/right keys.
    ///  In practice: it&#39;s complicated; mods are often provided from different sources. Keyboard layout, IME, sticky keys and
    ///  backends tend to interfere and break that equivalence. The safer decision is to relay that ambiguity down to the end-user...
    ///  - On macOS, we swap Cmd(Super) and Ctrl keys at the time of the io.AddKeyEvent() call.
    ///</summary>
    None = ImGuiKey.ModNone ,

    ///<summary>Ctrl (non-macOS), Cmd (macOS)</summary>
    Ctrl = ImGuiKey.ModCtrl,

    ///<summary>Shift</summary>
    Shift = ImGuiKey.ModShift,

    ///<summary>Option/Menu</summary>
    Alt = ImGuiKey.ModAlt,

    ///<summary>Windows/Super (non-macOS), Ctrl (macOS)</summary>
    Super = ImGuiKey.ModSuper,

    ///<summary>4-bits</summary>
    Mask = ImGuiKey.ModMask,
}

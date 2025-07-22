namespace NovaImGuiNet;

public record struct ImGuiKeyChord {
    public int Chord;

    public ImGuiKey Key {
        get => (ImGuiKey) (Chord & (int) ~ImGuiMod.Mask);
        set => Chord = ((int) value & (int) ~ImGuiMod.Mask) | (int) Modifiers;
    }

    public ImGuiMod Modifiers {
        get => (ImGuiMod) (Chord & (int) ImGuiMod.Mask);
        set => Chord = ((int) value & (int) ImGuiMod.Mask) | (int) Key;
    }

    public ImGuiKeyChord(int chord) {
        Chord = chord;
    }

    public ImGuiKeyChord(ImGuiMod modifiers, ImGuiKey key) {
        Key = key;
        Modifiers = modifiers;
    }

    public override string ToString() =>
        $"{nameof(Key)}: {Key}, {nameof(Modifiers)}: {Modifiers}, {nameof(Chord)}: {Chord:x8}";

    public static implicit operator int(ImGuiKeyChord chord) => chord.Chord;

    public static explicit operator ImGuiKeyChord(int chord) => new(chord);
    public static explicit operator ImGuiKeyChord(ImGuiKey key) => new((int) key);
}

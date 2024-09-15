#nullable enable

namespace NovaImGuiNet;

public partial struct ImColor {
    public ImColor(ImVec4 value) {
        Value = value;
    }

    public ImColor(float r, float g, float b, float a = 1f) : this(new ImVec4(r, g, b, a)) {
    }

    public ImColor(byte r, byte g, byte b, byte a = 255)
        : this(
            r * (1f / Byte.MaxValue),
            g * (1f / Byte.MaxValue),
            b * (1f / Byte.MaxValue),
            a * (1f / Byte.MaxValue)
        ) {
    }

    public ImColor(ImU32 rgba) : this(
        (byte) (rgba >> IM_COL32_R_SHIFT & 0xFF),
        (byte) (rgba >> IM_COL32_G_SHIFT & 0xFF),
        (byte) (rgba >> IM_COL32_B_SHIFT & 0xFF),
        (byte) (rgba >> IM_COL32_A_SHIFT & 0xFF)
    ) {
    }

    public uint ToUInt32() => ImGui.ColorConvertFloat4ToU32(Value);

    public override string ToString() =>
        $"R: {Value.X:0.000}, G: {Value.Y:0.000}, B: {Value.Z:0.000}, A: {Value.W:0.000}";

    public static implicit operator ImColor(ImVec4 value) => new(value);

    public static implicit operator ImU32(ImColor value) => value.ToUInt32();

    public static implicit operator ImVec4(ImColor value) => value.Value;
}

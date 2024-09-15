namespace NovaImGuiNet;

/// <summary>
/// Represent a boolean stored as a single byte.
/// Used in some edge-cases where regular bool type is not accepted
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 1)]
public struct ByteBool : IEquatable<ByteBool> {
    public byte RawValue;

    public bool Value {
        get => RawValue != 0;
        set => RawValue = (byte) (value ? 1 : 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBool(byte rawValue) {
        RawValue = rawValue;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ByteBool(bool value) {
        RawValue = (byte) (value ? 1 : 0);
    }

    public bool Equals(ByteBool other) {
        return Value == other.Value;
    }

    public override bool Equals(object? obj) {
        return obj is ByteBool other && Equals(other);
    }

    public override int GetHashCode() {
        return Value.GetHashCode();
    }

    public static bool operator ==(ByteBool left, ByteBool right) {
        return left.Equals(right);
    }

    public static bool operator !=(ByteBool left, ByteBool right) {
        return !left.Equals(right);
    }

    public override string ToString() => Value.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ByteBool(bool value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(ByteBool byteBool) => byteBool.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ByteBool(byte value) => new(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator ByteBool(sbyte value) => new((byte) value);
}

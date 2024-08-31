namespace NovaImGuiNet;

public struct ImFixedArray3<T> : IEquatable<ImFixedArray3<T>> where T : unmanaged {
    public T Value1;
    public T Value2;
    public T Value3;

    public ImFixedArray3(T value1, T value2, T value3) {
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
    }

    public bool Equals(ImFixedArray3<T> other) {
        return
            Value1.Equals(other.Value1) &&
            Value2.Equals(other.Value2) &&
            Value3.Equals(other.Value3);
    }

    public override bool Equals(object? obj) {
        return obj is ImFixedArray3<T> other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            int hashCode = Value1.GetHashCode();
            hashCode = (hashCode * 397) ^ Value2.GetHashCode();
            hashCode = (hashCode * 397) ^ Value3.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(ImFixedArray3<T> left, ImFixedArray3<T> right) {
        return left.Equals(right);
    }

    public static bool operator !=(ImFixedArray3<T> left, ImFixedArray3<T> right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return
            $"{nameof(Value1)}: {Value1}, " +
            $"{nameof(Value2)}: {Value2}, " +
            $"{nameof(Value3)}: {Value3}";
    }
}

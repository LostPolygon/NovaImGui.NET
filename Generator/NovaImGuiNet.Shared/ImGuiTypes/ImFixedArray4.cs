namespace NovaImGuiNet;

public struct ImFixedArray4<T> : IEquatable<ImFixedArray4<T>> where T : unmanaged {
    public T Value1;
    public T Value2;
    public T Value3;
    public T Value4;

    public ImFixedArray4(T value1, T value2, T value3, T value4) {
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
        Value4 = value4;
    }

    public bool Equals(ImFixedArray4<T> other) {
        return
            Value1.Equals(other.Value1) &&
            Value2.Equals(other.Value2) &&
            Value3.Equals(other.Value3) &&
            Value4.Equals(other.Value4);
    }

    public override bool Equals(object? obj) {
        return obj is ImFixedArray4<T> other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            int hashCode = Value1.GetHashCode();
            hashCode = (hashCode * 397) ^ Value2.GetHashCode();
            hashCode = (hashCode * 397) ^ Value3.GetHashCode();
            hashCode = (hashCode * 397) ^ Value4.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(ImFixedArray4<T> left, ImFixedArray4<T> right) {
        return left.Equals(right);
    }

    public static bool operator !=(ImFixedArray4<T> left, ImFixedArray4<T> right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return
            $"{nameof(Value1)}: {Value1}, " +
            $"{nameof(Value2)}: {Value2}, " +
            $"{nameof(Value3)}: {Value3}, " +
            $"{nameof(Value4)}: {Value4}";
    }
}

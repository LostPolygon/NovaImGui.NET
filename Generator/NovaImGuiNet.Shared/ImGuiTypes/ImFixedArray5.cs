namespace NovaImGuiNet;

public struct ImFixedArray5<T> : IEquatable<ImFixedArray5<T>> where T : unmanaged {
    public T Value1;
    public T Value2;
    public T Value3;
    public T Value4;
    public T Value5;

    public ImFixedArray5(T value1, T value2, T value3, T value4, T value5) {
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
        Value4 = value4;
        Value5 = value5;
    }

    public bool Equals(ImFixedArray5<T> other) {
        return
            Value1.Equals(other.Value1) &&
            Value2.Equals(other.Value2) &&
            Value3.Equals(other.Value3) &&
            Value4.Equals(other.Value4) &&
            Value5.Equals(other.Value5);
    }

    public override bool Equals(object? obj) {
        return obj is ImFixedArray5<T> other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            int hashCode = Value1.GetHashCode();
            hashCode = (hashCode * 397) ^ Value2.GetHashCode();
            hashCode = (hashCode * 397) ^ Value3.GetHashCode();
            hashCode = (hashCode * 397) ^ Value4.GetHashCode();
            hashCode = (hashCode * 397) ^ Value5.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(ImFixedArray5<T> left, ImFixedArray5<T> right) {
        return left.Equals(right);
    }

    public static bool operator !=(ImFixedArray5<T> left, ImFixedArray5<T> right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return
            $"{nameof(Value1)}: {Value1}, " +
            $"{nameof(Value2)}: {Value2}, " +
            $"{nameof(Value3)}: {Value3}, " +
            $"{nameof(Value4)}: {Value4}, " +
            $"{nameof(Value5)}: {Value5}";
    }
}

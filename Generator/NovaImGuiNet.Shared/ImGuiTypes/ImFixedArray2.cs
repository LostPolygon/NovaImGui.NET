namespace NovaImGuiNet;

public struct ImFixedArray2<T> : IEquatable<ImFixedArray2<T>> where T : unmanaged {
    public T Value1;
    public T Value2;

    public ImFixedArray2(T value1, T value2) {
        Value1 = value1;
        Value2 = value2;
    }

    public bool Equals(ImFixedArray2<T> other) {
        return
            Value1.Equals(other.Value1) &&
            Value2.Equals(other.Value2);
    }

    public override bool Equals(object? obj) {
        return obj is ImFixedArray2<T> other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            int hashCode = Value1.GetHashCode();
            hashCode = (hashCode * 397) ^ Value2.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(ImFixedArray2<T> left, ImFixedArray2<T> right) {
        return left.Equals(right);
    }

    public static bool operator !=(ImFixedArray2<T> left, ImFixedArray2<T> right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return
            $"{nameof(Value1)}: {Value1}, " +
            $"{nameof(Value2)}: {Value2}";
    }
}

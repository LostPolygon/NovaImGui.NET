namespace NovaImGuiNet;

public struct ImFixedArray20<T> : IEquatable<ImFixedArray20<T>> where T : unmanaged {
    public T Value1;
    public T Value2;
    public T Value3;
    public T Value4;
    public T Value5;
    public T Value6;
    public T Value7;
    public T Value8;
    public T Value9;
    public T Value10;
    public T Value11;
    public T Value12;
    public T Value13;
    public T Value14;
    public T Value15;
    public T Value16;
    public T Value17;
    public T Value18;
    public T Value19;
    public T Value20;

    public ImFixedArray20(T value1, T value2, T value3, T value4, T value5, T value6, T value7, T value8, T value9, T value10, T value11, T value12, T value13, T value14, T value15, T value16, T value17, T value18, T value19, T value20) {
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
        Value4 = value4;
        Value5 = value5;
        Value6 = value6;
        Value7 = value7;
        Value8 = value8;
        Value9 = value9;
        Value10 = value10;
        Value11 = value11;
        Value12 = value12;
        Value13 = value13;
        Value14 = value14;
        Value15 = value15;
        Value16 = value16;
        Value17 = value17;
        Value18 = value18;
        Value19 = value19;
        Value20 = value20;
    }

    public bool Equals(ImFixedArray20<T> other) {
        return
            Value1.Equals(other.Value1) &&
            Value2.Equals(other.Value2) &&
            Value3.Equals(other.Value3) &&
            Value4.Equals(other.Value4) &&
            Value5.Equals(other.Value5) &&
            Value6.Equals(other.Value6) &&
            Value7.Equals(other.Value7) &&
            Value8.Equals(other.Value8) &&
            Value9.Equals(other.Value9) &&
            Value10.Equals(other.Value10) &&
            Value11.Equals(other.Value11) &&
            Value12.Equals(other.Value12) &&
            Value13.Equals(other.Value13) &&
            Value14.Equals(other.Value14) &&
            Value15.Equals(other.Value15) &&
            Value16.Equals(other.Value16) &&
            Value17.Equals(other.Value17) &&
            Value18.Equals(other.Value18) &&
            Value19.Equals(other.Value19) &&
            Value20.Equals(other.Value20);
    }

    public override bool Equals(object? obj) {
        return obj is ImFixedArray20<T> other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            int hashCode = Value1.GetHashCode();
            hashCode = (hashCode * 397) ^ Value2.GetHashCode();
            hashCode = (hashCode * 397) ^ Value3.GetHashCode();
            hashCode = (hashCode * 397) ^ Value4.GetHashCode();
            hashCode = (hashCode * 397) ^ Value5.GetHashCode();
            hashCode = (hashCode * 397) ^ Value6.GetHashCode();
            hashCode = (hashCode * 397) ^ Value7.GetHashCode();
            hashCode = (hashCode * 397) ^ Value8.GetHashCode();
            hashCode = (hashCode * 397) ^ Value9.GetHashCode();
            hashCode = (hashCode * 397) ^ Value10.GetHashCode();
            hashCode = (hashCode * 397) ^ Value11.GetHashCode();
            hashCode = (hashCode * 397) ^ Value12.GetHashCode();
            hashCode = (hashCode * 397) ^ Value13.GetHashCode();
            hashCode = (hashCode * 397) ^ Value14.GetHashCode();
            hashCode = (hashCode * 397) ^ Value15.GetHashCode();
            hashCode = (hashCode * 397) ^ Value16.GetHashCode();
            hashCode = (hashCode * 397) ^ Value17.GetHashCode();
            hashCode = (hashCode * 397) ^ Value18.GetHashCode();
            hashCode = (hashCode * 397) ^ Value19.GetHashCode();
            hashCode = (hashCode * 397) ^ Value20.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(ImFixedArray20<T> left, ImFixedArray20<T> right) {
        return left.Equals(right);
    }

    public static bool operator !=(ImFixedArray20<T> left, ImFixedArray20<T> right) {
        return !left.Equals(right);
    }

    public override string ToString() {
        return
            $"{nameof(Value1)}: {Value1}, " +
            $"{nameof(Value2)}: {Value2}, " +
            $"{nameof(Value3)}: {Value3}, " +
            $"{nameof(Value4)}: {Value4}, " +
            $"{nameof(Value5)}: {Value5}, " +
            $"{nameof(Value6)}: {Value6}, " +
            $"{nameof(Value7)}: {Value7}, " +
            $"{nameof(Value8)}: {Value8}, " +
            $"{nameof(Value9)}: {Value9}, " +
            $"{nameof(Value10)}: {Value10}, " +
            $"{nameof(Value11)}: {Value11}, " +
            $"{nameof(Value12)}: {Value12}, " +
            $"{nameof(Value13)}: {Value13}, " +
            $"{nameof(Value14)}: {Value14}, " +
            $"{nameof(Value15)}: {Value15}, " +
            $"{nameof(Value16)}: {Value16}, " +
            $"{nameof(Value17)}: {Value17}, " +
            $"{nameof(Value18)}: {Value18}, " +
            $"{nameof(Value19)}: {Value19}, " +
            $"{nameof(Value20)}: {Value20}";
    }
}

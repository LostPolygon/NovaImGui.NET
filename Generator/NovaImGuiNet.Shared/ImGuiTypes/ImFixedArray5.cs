using System.Collections;

namespace NovaImGuiNet;

public struct ImFixedArray5<T> : IList<T>, IEquatable<ImFixedArray5<T>> where T : unmanaged, IEquatable<T> {
    public T Value1;
    public T Value2;
    public T Value3;
    public T Value4;
    public T Value5;

    public T this[int index] {
        get {
            // Following trick can reduce the range check by one
            if ((uint) index >= 4)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray5 index must be between 0 and 4.");
            
            return Unsafe.Add(ref Value1, index);
        }
        set {
            // Following trick can reduce the range check by one
            if ((uint) index >= 4)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray5 index must be between 0 and 4.");
            
            Unsafe.Add(ref Value1, index) = value;
        }
    }

    public ImFixedArray5(T value1, T value2, T value3, T value4, T value5) {
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
        Value4 = value4;
        Value5 = value5;
    }

    public unsafe Span<T> AsSpan() {
        fixed (T* ptr = &Value1) {
            return new Span<T>(ptr, 5);
        }
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

    public override string ToString() {
        return
            $"{nameof(Value1)}: {Value1}, " +
            $"{nameof(Value2)}: {Value2}, " +
            $"{nameof(Value3)}: {Value3}, " +
            $"{nameof(Value4)}: {Value4}, " +
            $"{nameof(Value5)}: {Value5}";
    }

    #region IEnumerable<T>

    public IEnumerator<T> GetEnumerator() {
        // Hardly anyone will use this, so it's fine
        yield return Value1;
        yield return Value2;
        yield return Value3;
        yield return Value4;
        yield return Value5;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    #endregion

    #region IList<T>

    public int IndexOf(T item) {
        return AsSpan().IndexOf(item);
    }

    public void Insert(int index, T item) {
        throw new NotSupportedException();
    }

    public void RemoveAt(int index) {
        throw new NotSupportedException();
    }
    
    public void Add(T item) {
        throw new NotSupportedException();
    }

    public void Clear() {
        throw new NotSupportedException();
    }

    public bool Contains(T item) {
        throw new NotSupportedException();
    }

    public void CopyTo(T[] array, int arrayIndex) {
        throw new NotSupportedException();
    }

    public bool Remove(T item) {
        throw new NotSupportedException();
    }

    public int Count { get; } = 5;

    public bool IsReadOnly { get; } = false;

    #endregion

    public static implicit operator Span<T>(ImFixedArray5<T> array) => array.AsSpan();
    
    public static bool operator ==(ImFixedArray5<T> left, ImFixedArray5<T> right) {
        return left.Equals(right);
    }

    public static bool operator !=(ImFixedArray5<T> left, ImFixedArray5<T> right) {
        return !left.Equals(right);
    }
}

using System.Collections;

namespace NovaImGuiNet;

public struct ImFixedArray2<T> : IList<T>, IEquatable<ImFixedArray2<T>> where T : unmanaged, IEquatable<T> {
    public T Value1;
    public T Value2;

    public T this[int index] {
        get {
            // Following trick can reduce the range check by one
            if ((uint) index >= 1)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray2 index must be between 0 and 1.");
            
            return Unsafe.Add(ref Value1, index);
        }
        set {
            // Following trick can reduce the range check by one
            if ((uint) index >= 1)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray2 index must be between 0 and 1.");
            
            Unsafe.Add(ref Value1, index) = value;
        }
    }

    public ImFixedArray2(T value1, T value2) {
        Value1 = value1;
        Value2 = value2;
    }

    public unsafe Span<T> AsSpan() {
        fixed (T* ptr = &Value1) {
            return new Span<T>(ptr, 2);
        }
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

    public override string ToString() {
        return
            $"{nameof(Value1)}: {Value1}, " +
            $"{nameof(Value2)}: {Value2}";
    }

    #region IEnumerable<T>

    public IEnumerator<T> GetEnumerator() {
        // Hardly anyone will use this, so it's fine
        yield return Value1;
        yield return Value2;
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

    public int Count { get; } = 2;

    public bool IsReadOnly { get; } = false;

    #endregion

    public static implicit operator Span<T>(ImFixedArray2<T> array) => array.AsSpan();
    
    public static bool operator ==(ImFixedArray2<T> left, ImFixedArray2<T> right) {
        return left.Equals(right);
    }

    public static bool operator !=(ImFixedArray2<T> left, ImFixedArray2<T> right) {
        return !left.Equals(right);
    }
}

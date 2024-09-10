using System.Collections;
namespace NovaImGuiNet;

public record struct ImFixedArray5<T> : IList<T> where T : unmanaged, IEquatable<T> {
    public T Value1;
    public T Value2;
    public T Value3;
    public T Value4;
    public T Value5;

    public T this[int index] {
        get {
            if ((uint) index >= 4)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray5 index must be between 0 and 4.");
            
            return Unsafe.Add(ref Value1, index);
        }
        set {
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
        AsSpan().CopyTo(new Span<T>(array, arrayIndex, array.Length - arrayIndex));
    }

    public bool Remove(T item) {
        throw new NotSupportedException();
    }

    public int Count => 5;

    public bool IsReadOnly => false;

    #endregion

    public void Deconstruct(out T value1, out T value2, out T value3, out T value4, out T value5) {
        value1 = Value1;
        value2 = Value2;
        value3 = Value3;
        value4 = Value4;
        value5 = Value5;
    }

    public static implicit operator Span<T>(ImFixedArray5<T> array) => array.AsSpan();
}

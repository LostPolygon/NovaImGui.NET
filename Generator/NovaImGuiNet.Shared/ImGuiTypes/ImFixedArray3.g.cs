using System.Collections;
namespace NovaImGuiNet;

public record struct ImFixedArray3<T> : IList<T> where T : unmanaged, IEquatable<T> {
    public T Value1;
    public T Value2;
    public T Value3;

    public T this[int index] {
        get {
            if ((uint) index >= 2)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray3 index must be between 0 and 2.");
            
            return Unsafe.Add(ref Value1, index);
        }
        set {
            if ((uint) index >= 2)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray3 index must be between 0 and 2.");
            
            Unsafe.Add(ref Value1, index) = value;
        }
    }

    public ImFixedArray3(T value1, T value2, T value3) {
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
    }

    public unsafe Span<T> AsSpan() {
        fixed (T* ptr = &Value1) {
            return new Span<T>(ptr, 3);
        }
    }

    #region IEnumerable<T>

    public IEnumerator<T> GetEnumerator() {
        // Hardly anyone will use this, so it's fine
        yield return Value1;
        yield return Value2;
        yield return Value3;
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

    public int Count => 3;

    public bool IsReadOnly => false;

    #endregion

    public void Deconstruct(out T value1, out T value2, out T value3) {
        value1 = Value1;
        value2 = Value2;
        value3 = Value3;
    }

    public static implicit operator Span<T>(ImFixedArray3<T> array) => array.AsSpan();
}

using System.Collections;
namespace NovaImGuiNet;

public record struct ImFixedArray20<T> : IList<T> where T : unmanaged, IEquatable<T> {
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

    public T this[int index] {
        get {
            if ((uint) index >= 19)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray20 index must be between 0 and 19.");
            
            return Unsafe.Add(ref Value1, index);
        }
        set {
            if ((uint) index >= 19)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray20 index must be between 0 and 19.");
            
            Unsafe.Add(ref Value1, index) = value;
        }
    }

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

    public unsafe Span<T> AsSpan() {
        fixed (T* ptr = &Value1) {
            return new Span<T>(ptr, 20);
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
        yield return Value6;
        yield return Value7;
        yield return Value8;
        yield return Value9;
        yield return Value10;
        yield return Value11;
        yield return Value12;
        yield return Value13;
        yield return Value14;
        yield return Value15;
        yield return Value16;
        yield return Value17;
        yield return Value18;
        yield return Value19;
        yield return Value20;
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

    public int Count => 20;

    public bool IsReadOnly => false;

    #endregion

    public static implicit operator Span<T>(ImFixedArray20<T> array) => array.AsSpan();
}

#nullable enable

using System.Collections;

namespace NovaImGuiNet;

public unsafe struct ImVector<T> : IList<T>, IEquatable<ImVector<T>>, IDisposable where T : unmanaged {
    public int Size;
    public int Capacity;
    public T* Data;

    public IntPtr DataIntPtr => (IntPtr) Data;

    public Span<T> AsSpan() => new(Data, Size);

    public ref T this[int index] => ref Unsafe.AsRef<T>((void*) AddressAt(index));

    public ImVector() {
        this = NativeConstruct();
    }

    public ImVector(ImVector<T> vector) : this() {
        Size = vector.Size;
        Capacity = vector.Capacity;
        Data = vector.Data;
    }

    public ImVector(int size, int capacity, T* data) : this() {
        Size = size;
        Capacity = capacity;
        Data = data;
    }

    public IntPtr AddressAt(int index) {
        if ((uint) index >= Size) {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                $"Index must be smaller than {nameof(ImVector<T>)}.{nameof(Size)}"
            );
        }

        return (IntPtr) (&Data[index]);
    }

    public void Dispose() {
        NativeDestroy(ref this);
    }

    public static ImVector<T> NativeConstruct() {
        ImVector<T> result = default;
        ImGuiNative.ImVector_Construct((IntPtr) (&result));
        return result;
    }

    public static void NativeDestroy(ref ImVector<T> vector) {
        ImGuiNative.ImVector_Destruct((IntPtr) Unsafe.AsPointer(ref vector));
    }

    public bool Equals(ImVector<T> other) => Size == other.Size && Capacity == other.Capacity && Data == other.Data;

    public override bool Equals(object? obj) => obj is ImVector<T> other && Equals(other);

    public override int GetHashCode() {
        unchecked {
            int hashCode = Size;
            hashCode = (hashCode * 397) ^ Capacity;
            hashCode = (hashCode * 397) ^ unchecked((int) (long) Data);
            return hashCode;
        }
    }

    T IList<T>.this[int index] {
        get => this[index];
        set => this[index] = value;
    }

    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>) this).GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        Size == 0 ?
            Enumerable.Empty<T>().GetEnumerator() :
            GetEnumerator();

    public bool Contains(T item) {
        return IndexOf(item) != -1;
    }

    public void CopyTo(T[] array, int arrayIndex) => AsSpan().CopyTo(array.AsSpan(arrayIndex));

    public int Count => Size;

    public bool IsReadOnly => false;

    public int IndexOf(T item) {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        Span<T> span = AsSpan();
        for (int index = 0; index < span.Length; index++) {
            if (comparer.Equals(span[index], item))
                return index;
        }

        return -1;
    }

    public void Add(T item) {
        throw new NotSupportedException();
    }

    public bool Remove(T item) {
        throw new NotSupportedException();
    }

    public void Clear() {
        throw new NotSupportedException();
    }

    public void Insert(int index, T item) {
        throw new NotSupportedException();
    }

    public void RemoveAt(int index) {
        throw new NotSupportedException();
    }

    public static implicit operator Span<T>(ImVector<T> vector) => vector.AsSpan();

    public static bool operator ==(ImVector<T> left, ImVector<T> right) => left.Equals(right);

    public static bool operator !=(ImVector<T> left, ImVector<T> right) => !left.Equals(right);

    public struct Enumerator : IEnumerator<T> {
        private readonly ImVector<T> _vector;
        private int _index;
        private T _current;

        public Enumerator(ImVector<T> vector) {
            _vector = vector;
            _index = 0;
            _current = default;
        }

        public bool MoveNext() {
            if (_index < _vector.Size) {
                _current = _vector.Data[_index];
                _index++;
                return true;
            }

            _index = _vector.Size + 1;
            _current = default;
            return false;
        }

        public void Reset() {
            _index = 0;
            _current = default;
        }

        public T Current => _current;

        object IEnumerator.Current {
            get {
                if (_index == 0 || _index == _vector.Size + 1)
                    throw new InvalidOperationException("Enumeration has not started or already finished.");

                return Current;
            }
        }

        public void Dispose() {
        }
    }
}

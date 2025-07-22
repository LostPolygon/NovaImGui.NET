#nullable enable

using System.Collections;

namespace NovaImGuiNet;

public unsafe struct ImStableVector32<T> : IList<T>, IEquatable<ImStableVector32<T>> where T : unmanaged {
    public const int BLOCK_SIZE = 32;

    public int Size;
    public int Capacity;
    public ImVector<Pointer<ImFontBaked>> Blocks;

    public ref T this[int index] => ref *(T*) AddressAt(index);

    public ImStableVector32() {
        this = default;
    }

    public ImStableVector32(ImStableVector32<T> vector) : this() {
        Size = vector.Size;
        Capacity = vector.Capacity;
        Blocks = vector.Blocks;
    }

    public ImStableVector32(int size, int capacity, ImVector<Pointer<ImFontBaked>> blocks) : this() {
        Size = size;
        Capacity = capacity;
        Blocks = blocks;
    }

    public IntPtr AddressAt(int index) {
        if ((uint) index >= Size) {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                $"Index must be smaller than {nameof(ImStableVector32<T>)}.{nameof(Size)}"
            );
        }

        return Blocks[index / BLOCK_SIZE][index % BLOCK_SIZE].IntPtr;
    }

    public bool Equals(ImStableVector32<T> other) => Size == other.Size && Capacity == other.Capacity && Blocks == other.Blocks;

    public override bool Equals(object? obj) => obj is ImStableVector32<T> other && Equals(other);

    public override int GetHashCode() {
        unchecked {
            int hashCode = Size;
            hashCode = (hashCode * 397) ^ Capacity;
            hashCode = (hashCode * 397) ^ Blocks.GetHashCode();
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

    public void CopyTo(T[] array, int arrayIndex) => this.ToArray().CopyTo(array.AsSpan(arrayIndex));

    public int Count => Size;

    public bool IsReadOnly => false;

    public int IndexOf(T item) {
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;

        for (int index = 0; index < Size; index++) {
            if (comparer.Equals(this[index], item))
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

    public static bool operator ==(ImStableVector32<T> left, ImStableVector32<T> right) => left.Equals(right);

    public static bool operator !=(ImStableVector32<T> left, ImStableVector32<T> right) => !left.Equals(right);

    public struct Enumerator : IEnumerator<T> {
        private readonly ImStableVector32<T> _vector;
        private int _index;
        private T _current;

        public Enumerator(ImStableVector32<T> vector) {
            _vector = vector;
            _index = 0;
            _current = default;
        }

        public bool MoveNext() {
            if (_index < _vector.Size) {
                _current = _vector[_index];
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

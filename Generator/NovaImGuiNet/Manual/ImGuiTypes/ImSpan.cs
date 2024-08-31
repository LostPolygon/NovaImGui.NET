using System.Collections;
using System.Runtime.CompilerServices;

namespace NovaImGuiNet;

public readonly unsafe struct ImSpan<T> : IEnumerable<T> where T : unmanaged {
    public readonly T* Data;
    public readonly T* DataEnd;

    public IntPtr DataIntPtr => (IntPtr) Data;

    public IntPtr DataEndIntPtr => (IntPtr) DataEnd;

    public long Count => DataEnd - Data;

    public Span<T> AsSpan() => new(Data, (int) Count);

    public ref T this[int index] => ref Unsafe.AsRef<T>((void*) AddressAt(index));

    public ImSpan(T* data, T* dataEnd) {
        Data = data;
        DataEnd = dataEnd;
    }

    public IntPtr AddressAt(int index) => (IntPtr) (&Data[index]);

    public Enumerator GetEnumerator() => new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        Count == 0 ?
            Enumerable.Empty<T>().GetEnumerator() :
            GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>) this).GetEnumerator();

    public struct Enumerator : IEnumerator<T> {
        private readonly ImSpan<T> _span;
        private long _index;
        private T _current;

        public Enumerator(ImSpan<T> span) {
            _span = span;
            _index = 0;
            _current = default;
        }

        public bool MoveNext() {
            if (_index < _span.Count) {
                _current = _span.Data[_index];
                _index++;
                return true;
            }

            _index = _span.Count + 1;
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
                if (_index == 0 || _index == _span.Count + 1)
                    throw new InvalidOperationException("Enumeration has not started or already finished.");

                return Current;
            }
        }

        public void Dispose() {
        }
    }
}

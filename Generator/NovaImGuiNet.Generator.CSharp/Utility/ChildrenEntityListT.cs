using System.Collections;

namespace NovaImGuiNet.Generator.CSharp;

public abstract class ChildrenEntityList<TBase> : IList<TBase> {
    private readonly List<TBase> _list = [];

    public IReadOnlyList<Type> AllowedItemTypes { get; }

    protected ChildrenEntityList(IReadOnlyList<Type> allowedItemTypes) {
        AllowedItemTypes = allowedItemTypes;
    }

    public IEnumerator<TBase> GetEnumerator() {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return ((IEnumerable) _list).GetEnumerator();
    }

    public void Add(TBase item) {
        ThrowOnInvalidItemType(item);
        _list.Add(item);
    }

    public void AddRange(IEnumerable<TBase> items) {
        foreach (TBase item in items) {
            Add(item);
        }
    }

    public void Clear() {
        _list.Clear();
    }

    public bool Contains(TBase item) {
        return _list.Contains(item);
    }

    public void CopyTo(TBase[] array, int arrayIndex) {
        _list.CopyTo(array, arrayIndex);
    }

    public bool Remove(TBase item) {
        return _list.Remove(item);
    }

    public int Count => _list.Count;

    public bool IsReadOnly => ((IList<TBase>) _list).IsReadOnly;

    public int IndexOf(TBase item) {
        return _list.IndexOf(item);
    }

    public void Insert(int index, TBase item) {
        ThrowOnInvalidItemType(item);
        _list.Insert(index, item);
    }

    public void RemoveAt(int index) {
        _list.RemoveAt(index);
    }

    public TBase this[int index] {
        get => _list[index];
        set {
            ThrowOnInvalidItemType(value);
            _list[index] = value;
        }
    }

    private void ThrowOnInvalidItemType(TBase? entity) {
        ArgumentNullException.ThrowIfNull(entity);

        Type type = entity.GetType();
        if (!AllowedItemTypes.Any(allowedType => type.IsAssignableTo(allowedType)))
            throw new InvalidOperationException(
                $"Type {type.FullName} is not a valid child entity type. " +
                $"Allowed types: [{String.Join(", ", AllowedItemTypes.Select(t => t.FullName))}]"
            );
    }
}

namespace NovaImGuiNet.Generator;

public class EqualityList<T> : List<T>, IEquatable<EqualityList<T>> {
    public EqualityList() {
    }

    public EqualityList(IEnumerable<T> collection) : base(collection) {
    }

    public EqualityList(int capacity) : base(capacity) {
    }

    public bool Equals(EqualityList<T>? other) {
        if (other == null)
            return false;

        return ReferenceEquals(other, this) || this.SequenceEqual(other);
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || (obj is EqualityList<T> other && Equals(other));
    }

    public override int GetHashCode() {
        HashCode hashCode = new();
        for (int i = 0; i < Count; i++) {
            hashCode.Add(this[i]);
        }

        return hashCode.ToHashCode();
    }
}

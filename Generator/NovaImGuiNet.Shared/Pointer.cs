using System.Runtime.CompilerServices;

namespace NovaImGuiNet;

/// <summary>
/// Wrapper for a typed pointer, for use as a generic parameter,
/// since .NET doesn't support pointers as generic arguments.
/// </summary>
/// <typeparam name="T"></typeparam>
public unsafe struct Pointer<T> : IEquatable<Pointer<T>> where T : unmanaged {
    public T* Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pointer(T* ptr) => Ptr = ptr;

    public ref T Value {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref *Ptr;
    }

    public IntPtr ToIntPtr() => (IntPtr) Ptr;

    public override string ToString() {
        return ToIntPtr().ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Pointer<T> other) => Ptr == other.Ptr;

    public override bool Equals(object? obj) => obj is Pointer<T> other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => ((IntPtr) Ptr).GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Pointer<T> left, Pointer<T> right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Pointer<T> left, Pointer<T> right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator IntPtr(Pointer<T> ptr) => (IntPtr) ptr.Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(Pointer<T> ptr) => ptr.Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pointer<T>(T* ptr) => new(ptr);
}

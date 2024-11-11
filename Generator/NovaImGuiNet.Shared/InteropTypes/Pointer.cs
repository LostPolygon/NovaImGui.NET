using System.Diagnostics;

namespace NovaImGuiNet;

/// <summary>
/// Wrapper for a typed pointer, for use as a generic parameter,
/// since .NET doesn't support pointers as generic arguments.
/// </summary>
/// <typeparam name="T"></typeparam>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Pointer<T> : IEquatable<Pointer<T>> where T : unmanaged {
    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    public T* Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pointer(T* ptr) => Ptr = ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Pointer(IntPtr ptr) => Ptr = (T*) ptr;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public IntPtr IntPtr => (IntPtr) Ptr;

    public bool HasValue {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Ptr != null;
    }

    public Pointer<T> this[int index] {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Ptr + index * sizeof(T);
    }

    public ref T Value {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get {
            if (Ptr == null) {
                InteropExceptions.ThrowNativePointerNull<T>();
            }

            return ref *Ptr;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetValueUnsafe() => ref *Ptr;

    public override string ToString() {
        return IntPtr != IntPtr.Zero ? $"0x{IntPtr:x8}" : "null";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Pointer<T> other) => Ptr == other.Ptr;

    public override bool Equals(object? obj) => obj is Pointer<T> other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => IntPtr.GetHashCode();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Pointer<T> left, Pointer<T> right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Pointer<T> left, Pointer<T> right) => !left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator IntPtr(Pointer<T> ptr) => ptr.IntPtr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(Pointer<T> ptr) => ptr.Ptr;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Pointer<T>(T* ptr) => new(ptr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Pointer<T>(IntPtr ptr) => new(ptr);
}

using System.Diagnostics;

namespace NovaImGuiNet.Interop;

/// <summary>
/// Managed wrapper for an growth-only managed memory buffer.
/// </summary>
public sealed class NativeMemoryManagedBuffer : IDisposable {
    private IntPtr _memory;
    private UIntPtr _allocatedByteCount;

    public IntPtr Memory => _memory;

    public UIntPtr AllocatedByteCount => _allocatedByteCount;

    public NativeMemoryManagedBuffer(nuint initialSize) {
        SetSize(initialSize);
    }

    public IntPtr EnsureSize(nuint byteCount) {
        if (_allocatedByteCount >= byteCount)
            return _memory;

        SetSize(byteCount);
        return _memory;
    }

    public override string ToString() {
        return $"{nameof(Memory)}: 0x{Memory:x8}, {nameof(AllocatedByteCount)}: {AllocatedByteCount}";
    }

    private IntPtr SetSize(nuint byteCount) {
        if (byteCount == UIntPtr.Zero)
            throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount, "Size cannot be zero.");

        Debug.Assert(_allocatedByteCount != byteCount);
        _allocatedByteCount = byteCount;
        _memory = InteropUtility.NativeMemoryReallocate(_memory, byteCount);

        return _memory;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Free() {
        if (_memory != IntPtr.Zero) {
            InteropUtility.NativeMemoryFree(_memory);
            _memory = IntPtr.Zero;
        }
    }

    private void ReleaseUnmanagedResources() {
        Free();
    }

    public void Dispose() {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~NativeMemoryManagedBuffer() {
        ReleaseUnmanagedResources();
    }
}

using System.Diagnostics;

namespace NovaImGuiNet.Interop;

/// <summary>
/// Managed wrapper for a growth-only managed memory buffer.
/// </summary>
public sealed class NativeMemoryManagedBuffer : IDisposable {
    private IntPtr _memoryPtr;
    private UIntPtr _allocatedByteCount;

    public IntPtr MemoryPtr {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _memoryPtr;
    }

    public UIntPtr AllocatedByteCount {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _allocatedByteCount;
    }

    public NativeMemoryManagedBuffer(nuint initialSize) {
        SetSize(initialSize);
    }

    public void EnsureSize(nuint byteCount) {
        if (_allocatedByteCount >= byteCount)
            return;

        SetSize(byteCount);
    }

    public override string ToString() {
        return $"{nameof(MemoryPtr)}: 0x{MemoryPtr:x8}, {nameof(AllocatedByteCount)}: {AllocatedByteCount}";
    }

    private IntPtr SetSize(nuint byteCount) {
        if (byteCount == UIntPtr.Zero)
            throw new ArgumentOutOfRangeException(nameof(byteCount), byteCount, "Size cannot be zero.");

        Debug.Assert(_allocatedByteCount != byteCount);
        _allocatedByteCount = byteCount;
        _memoryPtr = InteropUtility.NativeMemoryReallocate(_memoryPtr, byteCount);

        return _memoryPtr;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Free() {
        if (_memoryPtr != IntPtr.Zero) {
            InteropUtility.NativeMemoryFree(_memoryPtr);
            _memoryPtr = IntPtr.Zero;
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

#nullable enable
namespace NovaImGuiNet;

public unsafe partial struct ImGuiPayload {
    public ReadOnlySpan<TDataItem> GetData<TDataItem>() where TDataItem : unmanaged {
        if (Data == IntPtr.Zero)
            throw new InvalidOperationException("Payload data is null.");

        int itemCount = DataSize / sizeof(TDataItem);
        return new ReadOnlySpan<TDataItem>((void*) Data, itemCount);
    }
}

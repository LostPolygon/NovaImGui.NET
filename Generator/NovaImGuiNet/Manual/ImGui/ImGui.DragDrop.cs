#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    ///<summary>type is a user defined string of maximum 32 characters. Strings starting with &#39;_&#39; are reserved for dear imgui internal types. Data is copied and held by imgui. Return true when payload has been accepted.</summary>
    public static bool SetDragDropPayload<TDataItem>(
        ReadOnlySpan<byte> type,
        ReadOnlySpan<TDataItem> data,
        ImGuiCond cond = ImGuiCond.None
    ) where TDataItem : unmanaged {
        fixed (TDataItem* dataPtr = data) {
            return SetDragDropPayload(type, (IntPtr) dataPtr, data.Length * sizeof(TDataItem), cond);
        }
    }

    ///<summary>type is a user defined string of maximum 32 characters. Strings starting with &#39;_&#39; are reserved for dear imgui internal types. Data is copied and held by imgui. Return true when payload has been accepted.</summary>
    public static bool SetDragDropPayload<TDataItem>(
        ReadOnlySpan<char> type,
        ReadOnlySpan<TDataItem> data,
        ImGuiCond cond = ImGuiCond.None
    ) where TDataItem : unmanaged {
        fixed (TDataItem* dataPtr = data) {
            return SetDragDropPayload(type, (IntPtr) dataPtr, data.Length * sizeof(TDataItem), cond);
        }
    }

#if NETSTANDARD2_0
    ///<summary>type is a user defined string of maximum 32 characters. Strings starting with &#39;_&#39; are reserved for dear imgui internal types. Data is copied and held by imgui. Return true when payload has been accepted.</summary>
    public static bool SetDragDropPayload<TDataItem>(
        string type,
        ReadOnlySpan<TDataItem> data,
        ImGuiCond cond = ImGuiCond.None
    ) where TDataItem : unmanaged {
        fixed (TDataItem* dataPtr = data) {
            return SetDragDropPayload(type, (IntPtr) dataPtr, data.Length * sizeof(TDataItem), cond);
        }
    }
#endif
}

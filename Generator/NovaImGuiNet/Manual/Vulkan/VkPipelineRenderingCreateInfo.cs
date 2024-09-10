#if IMGUI_IMPL_VULKAN_HAS_DYNAMIC_RENDERING

namespace NovaImGuiNet.Backends.Vulkan;

public record struct VkPipelineRenderingCreateInfo {
    public int sType;

    public IntPtr pNext;

    public uint viewMask;

    public uint colorAttachmentCount;

    public IntPtr pColorAttachmentFormats;

    public int depthAttachmentFormat;

    public int stencilAttachmentFormat;
}

#endif

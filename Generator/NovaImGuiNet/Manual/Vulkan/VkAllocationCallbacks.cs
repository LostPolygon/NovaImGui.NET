namespace NovaImGuiNet.Backends.Vulkan;

using System;

/// <summary>Structure containing callback function pointers for memory allocation</summary>
public record struct VkAllocationCallbacks {
    /// <summary>
    /// is a value to be interpreted by the implementation of
    /// the callbacks.
    /// When any of the callbacks in <c>VkAllocationCallbacks</c> are called, the
    /// Vulkan implementation will pass this value as the first parameter to the
    /// callback.
    /// This value can: vary each time an allocator is passed into a command,
    /// even when the same object takes an allocator in multiple commands.
    /// </summary>
    public IntPtr pUserData;

    /// <summary>
    /// is a tlink:PFN_vkAllocationFunction pointer to an
    /// application-defined memory allocation function.
    /// </summary>
    public IntPtr pfnAllocation;

    /// <summary>
    /// is a tlink:PFN_vkReallocationFunction pointer to
    /// an application-defined memory reallocation function.
    /// </summary>
    public IntPtr pfnReallocation;

    /// <summary>
    /// is a tlink:PFN_vkFreeFunction pointer to an
    /// application-defined memory free function.
    /// </summary>
    public IntPtr pfnFree;

    /// <summary>
    /// is a
    /// tlink:PFN_vkInternalAllocationNotification pointer to an
    /// application-defined function that is called by the implementation when
    /// the implementation makes internal allocations.
    /// </summary>
    public IntPtr pfnInternalAllocation;

    /// <summary>
    /// is a tlink:PFN_vkInternalFreeNotification pointer
    /// to an application-defined function that is called by the implementation
    /// when the implementation frees internal allocations.
    /// </summary>
    public IntPtr pfnInternalFree;
}

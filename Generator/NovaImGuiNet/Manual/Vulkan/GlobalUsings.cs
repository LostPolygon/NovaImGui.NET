// VK dispatchable handles
global using VkInstance = nint;
global using VkPhysicalDevice = nint;
global using VkDevice = nint;
global using VkQueue = nint;
global using VkCommandPool = nint;
global using VkCommandBuffer = nint;

// VK non-dispatchable handles
global using VkDescriptorPool = System.UInt64;
global using VkRenderPass = System.UInt64;
global using VkPipelineCache = System.UInt64;
global using VkFence = System.UInt64;
global using VkImage = System.UInt64;
global using VkImageView = System.UInt64;
global using VkFramebuffer = System.UInt64;
global using VkSemaphore = System.UInt64;
global using VkSwapchainKHR = System.UInt64;
global using VkSurfaceKHR = System.UInt64;
global using VkPipeline = System.UInt64;
global using VkDescriptorSet = System.UInt64;
global using VkSampler = System.UInt64;

// VK misc
global using VkFormat = int;
global using VkColorSpaceKHR = int;
global using VkSampleCountFlagBits = int;
global using VkPresentModeKHR = int;
global using VkClearValue = int;
global using VkImageLayout = int;
global using VkResult = int;
global using VkDeviceSize = System.UInt64;

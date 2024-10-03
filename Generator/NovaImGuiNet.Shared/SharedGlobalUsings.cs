global using System.Runtime.InteropServices;
global using System.Runtime.CompilerServices;
global using NovaImGuiNet.Interop;

#if !NOVA_IMGUI_SHARED
global using Utf8BytesStringWriter = Utf8StringInterpolation.Utf8StringWriter<System.Buffers.ArrayBufferWriter<byte>>;
#endif

global using size_t = nint;
global using int32_t = System.Int32;
global using uint32_t = System.UInt32;
global using int64_t = System.Int64;
global using uint64_t = System.UInt64;
global using signed_byte = System.Byte;

#if !USE_UNITY_VECTORS
global using ImVec2 = System.Numerics.Vector2;
global using ImVec4 = System.Numerics.Vector4;

global using ImVec3 = System.Numerics.Vector3; // Doesn't exist in Dear ImGui, but convenient for consistency
#endif

#if !NOVA_IMGUI_NET_MAIN
global using unsafe ImPoolIdx = System.Int32;
#endif



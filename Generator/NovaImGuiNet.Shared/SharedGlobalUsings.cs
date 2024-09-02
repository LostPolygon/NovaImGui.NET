global using System.Runtime.InteropServices;
global using System.Runtime.CompilerServices;
global using size_t = nint;
global using int32_t = System.Int32;
global using int64_t = System.Int64;

//global using signed_byte = System.Byte;
//global using HWND = nint;
//global using UINT = nint;
//global using WPARAM = nint;
//global using LPARAM = nint;
//global using LRESULT = nint;

global using ImVec2 = System.Numerics.Vector2;
global using ImVec4 = System.Numerics.Vector4;

#if !NOVA_IMGUI_NET_MAIN
global using unsafe ImPoolIdx = System.Int32;
#endif

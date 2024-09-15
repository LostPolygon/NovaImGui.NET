#nullable enable

namespace NovaImGuiNet;

public static unsafe partial class ImGui {
    ///<summary>
    ///  Widgets: Color Editor/Picker (tip: the ColorEdit* functions have a little color square that can be left-clicked to open a picker, and right-clicked to open an option menu.)
    ///  - Note that in C++ a &#39;float v[X]&#39; function argument is the _same_ as &#39;float* v&#39;, the array syntax is just a way to document the number of elements that are expected to be accessible.
    ///  - You can pass the address of a first float element out of a contiguous structure, e.g. &amp;myvector.x
    ///</summary>
    public static bool ColorEdit3(ReadOnlySpan<byte> label, ImVec4* col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None) =>
        ColorEdit3(label, (ImVec3*) col, flags);

    ///<summary>
    ///  Widgets: Color Editor/Picker (tip: the ColorEdit* functions have a little color square that can be left-clicked to open a picker, and right-clicked to open an option menu.)
    ///  - Note that in C++ a &#39;float v[X]&#39; function argument is the _same_ as &#39;float* v&#39;, the array syntax is just a way to document the number of elements that are expected to be accessible.
    ///  - You can pass the address of a first float element out of a contiguous structure, e.g. &amp;myvector.x
    ///</summary>
    public static bool ColorEdit3(ReadOnlySpan<byte> label, ref ImVec4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None) =>
        ColorEdit3(label, ref Unsafe.As<ImVec4, ImVec3>(ref col), flags);

    ///<summary>
    ///  Widgets: Color Editor/Picker (tip: the ColorEdit* functions have a little color square that can be left-clicked to open a picker, and right-clicked to open an option menu.)
    ///  - Note that in C++ a &#39;float v[X]&#39; function argument is the _same_ as &#39;float* v&#39;, the array syntax is just a way to document the number of elements that are expected to be accessible.
    ///  - You can pass the address of a first float element out of a contiguous structure, e.g. &amp;myvector.x
    ///</summary>
    public static bool ColorEdit3(ReadOnlySpan<char> label, ImVec4* col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None) =>
        ColorEdit3(label, (ImVec3*) col, flags);

    ///<summary>
    ///  Widgets: Color Editor/Picker (tip: the ColorEdit* functions have a little color square that can be left-clicked to open a picker, and right-clicked to open an option menu.)
    ///  - Note that in C++ a &#39;float v[X]&#39; function argument is the _same_ as &#39;float* v&#39;, the array syntax is just a way to document the number of elements that are expected to be accessible.
    ///  - You can pass the address of a first float element out of a contiguous structure, e.g. &amp;myvector.x
    ///</summary>
    public static bool ColorEdit3(ReadOnlySpan<char> label, ref ImVec4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None) =>
        ColorEdit3(label, ref Unsafe.As<ImVec4, ImVec3>(ref col), flags);

#if NETSTANDARD2_0
    ///<summary>
    ///  Widgets: Color Editor/Picker (tip: the ColorEdit* functions have a little color square that can be left-clicked to open a picker, and right-clicked to open an option menu.)
    ///  - Note that in C++ a &#39;float v[X]&#39; function argument is the _same_ as &#39;float* v&#39;, the array syntax is just a way to document the number of elements that are expected to be accessible.
    ///  - You can pass the address of a first float element out of a contiguous structure, e.g. &amp;myvector.x
    ///</summary>
    public static bool ColorEdit3(string label, ImVec4* col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None) =>
        ColorEdit3(label, (ImVec3*) col, flags);

    ///<summary>
    ///  Widgets: Color Editor/Picker (tip: the ColorEdit* functions have a little color square that can be left-clicked to open a picker, and right-clicked to open an option menu.)
    ///  - Note that in C++ a &#39;float v[X]&#39; function argument is the _same_ as &#39;float* v&#39;, the array syntax is just a way to document the number of elements that are expected to be accessible.
    ///  - You can pass the address of a first float element out of a contiguous structure, e.g. &amp;myvector.x
    ///</summary>
    public static bool ColorEdit3(string label, ref ImVec4 col, ImGuiColorEditFlags flags = ImGuiColorEditFlags.None) =>
        ColorEdit3(label, ref Unsafe.As<ImVec4, ImVec3>(ref col), flags);
#endif
}

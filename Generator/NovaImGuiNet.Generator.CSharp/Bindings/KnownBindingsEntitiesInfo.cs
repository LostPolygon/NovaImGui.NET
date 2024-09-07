namespace NovaImGuiNet.Generator.CSharp;

public static class KnownBindingsEntitiesInfo {
    public static readonly IReadOnlyList<string> ExpectedTypesDefinedElsewhere = [
        // Dear ImGui
        "ImStr",

        // C standard lib
        "size_t",
        "varargs",
        "va_list",

        // Win32 backend
        "HWND",
        "UINT",
        "WPARAM",
        "LPARAM",
        "LRESULT",

        // SDL2 Backend
        "SDL_Event",
        "SDL_Renderer",

        // ImPlot
        "IMPLOT_API",
        "T",
    ];
}

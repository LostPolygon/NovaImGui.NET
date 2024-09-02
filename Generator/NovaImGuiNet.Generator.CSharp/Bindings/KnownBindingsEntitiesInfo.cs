namespace NovaImGuiNet.Generator.CSharp;

public static class KnownBindingsEntitiesInfo {
    public static readonly IReadOnlyList<string> ExpectedTypesDefinedElsewhere = [
        "size_t",
        "varargs",
        "va_list",

        "ImStr",

        "HWND",
        "UINT",
        "WPARAM",
        "LPARAM",
        "LRESULT",

        "SDL_Event",
        "SDL_Renderer",
    ];
}

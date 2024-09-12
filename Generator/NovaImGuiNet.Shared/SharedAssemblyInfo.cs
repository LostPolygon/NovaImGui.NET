#if NET6_0_OR_GREATER
// Minor optimization. Gotta be careful though!
[module: System.Runtime.CompilerServices.SkipLocalsInit]
#endif

// FIXME: commented out as runtime marshalling is required for passing managed callbacks
#if false && NET8_0_OR_GREATER
// ImGuiListClipperRange can't be used as a return value,
// since it contains a bool field and that's broken when
// runtime marshalling is not disabled (which is only supported on .NET 7+)
// https://learn.microsoft.com/en-us/dotnet/fundamentals/syslib-diagnostics/syslib1050-1069

[assembly: System.Runtime.CompilerServices.DisableRuntimeMarshalling]
#endif


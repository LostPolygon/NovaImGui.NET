namespace NovaImGuiNet.Generator.CSharp;

public record BindingsFileCustomizations {
    public string OriginalHeaderFileName { get; init; }

    public string RootNamespace { get; init; }
    public string Namespace { get; init; }
    public string ConstantsClassName { get; init; }
    public string NativeFunctionsClassName { get; init; }
    public string NativeFunctionsManagedWrapperClassName { get; init; }

    public IReadOnlyList<string> FunctionsNoPermutations { get; init; } = [];
    public IReadOnlyList<NativeFunctionArgumentsIdentifier> TreatPointerArgumentsAsArrays { get; init; } = [];
    public IReadOnlyList<string> PrefixesRemovedFromManagedMethods { get; init; } = [];
    public IReadOnlyList<string> MakePointersToStructsOpaque { get; init; } = [];
    public IReadOnlyList<string> NoEmitManagedMethodsForNativeFunctions { get; init; } = [];
    public IReadOnlyList<string> NoEmitNativeFunctions { get; init; } = [];
    public IReadOnlyList<string> NoEmitStructs { get; init; } = [];
    public IReadOnlyList<string> ExternalTypes { get; init; } = [];

    public record NativeFunctionIdentifier {
        public string? Struct { get; init; }
        public string Function { get; init; }
    }

    public record NativeFunctionArgumentsIdentifier : NativeFunctionIdentifier {
        public IReadOnlyList<string> Arguments { get; init; } = [];
    }
}

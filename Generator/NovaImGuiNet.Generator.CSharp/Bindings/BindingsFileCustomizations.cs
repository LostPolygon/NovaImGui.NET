namespace NovaImGuiNet.Generator.CSharp;

public record BindingsFileCustomizations {
    public string OriginalHeaderFileName { get; init; } = null!;

    public string RootNamespace { get; init; } = null!;
    public string Namespace { get; init; } = null!;
    public string ConstantsClassName { get; init; } = null!;
    public string NativeFunctionsClassName { get; init; } = null!;
    public string NativeFunctionsManagedWrapperClassName { get; init; } = null!;

    public IReadOnlyList<string> FunctionsNoPermutations { get; init; } = [];
    public IReadOnlyList<NativeFunctionArgumentsIdentifier> TreatPointerArgumentsAsArrays { get; init; } = [];
    public IReadOnlyList<string> PrefixesRemovedFromManagedMethods { get; init; } = [];
    public IReadOnlyList<string> MakePointersToStructsOpaque { get; init; } = [];
    public IReadOnlyList<string> NoEmitManagedMethodsForNativeFunctions { get; init; } = [];
    public IReadOnlyList<string> NoEmitNativeFunctions { get; init; } = [];
    public IReadOnlyList<string> NoEmitStructs { get; init; } = [];
    public IReadOnlyList<string> NoEmitTypedefs { get; init; } = [];
    public IReadOnlyList<TreatTypeAsHandle> GenerateHandleTypes { get; init; } = [];
    public IReadOnlyList<string> ExternalTypes { get; init; } = [];

    public record NativeFunctionIdentifier {
        public string? Struct { get; init; }
        public string Function { get; init; } = null!;
    }

    public record NativeFunctionArgumentsIdentifier : NativeFunctionIdentifier {
        public IReadOnlyList<string> Arguments { get; init; } = [];
    }

    public record TreatTypeAsHandle {
        public string Name { get; init; } = null!;
        public string? ValueType { get; init; }
    }
}

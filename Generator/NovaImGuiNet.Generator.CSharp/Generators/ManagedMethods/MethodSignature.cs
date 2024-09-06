namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public record MethodSignature(BindingsModel.Function NativeFunction, IReadOnlyList<ManagedMethodArgument> Arguments) {
    public override string ToString() {
        List<string> argumentDescriptions = [];
        for (int argumentIndex = 0; argumentIndex < Arguments.Count; argumentIndex++) {
            ManagedMethodArgument argument = Arguments[argumentIndex];

            string argumentDescription = $"[{argumentIndex + 1}] ";

            if (!argument.IsPartOfDeclaration) {
                argumentDescription += " (def-only)";
            } else {
                argumentDescription += $" ({argument.MarshalingVariant})";
            }

            argumentDescriptions.Add(argumentDescription);
        }

        return $"{NativeFunction.Name} ({String.Join(", ", argumentDescriptions)}";
    }
}

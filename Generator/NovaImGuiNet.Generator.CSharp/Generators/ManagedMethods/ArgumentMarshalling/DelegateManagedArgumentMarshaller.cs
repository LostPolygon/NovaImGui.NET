using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class DelegateManagedArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() {
        string value = Argument.MarshalledArgument.Name;
        string? defaultValue = null;
        if (!Argument.IsPartOfDeclaration) {
            Debug.Assert(Argument.MarshalledArgument.DefaultValue != null);
            defaultValue = Argument.MarshalledArgument.DefaultValue;
        }

        if (defaultValue != null && KnownEntities.NullishValues.Contains(defaultValue)) {
            return [$"IntPtr {InvocationVariableName} = default;"];
        }

        if (defaultValue != null) {
            value = defaultValue;
        }

        return [
            $"IntPtr {InvocationVariableName} = {value} != null ? Marshal.GetFunctionPointerForDelegate({value}) : default;"
        ];
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateUnmarshalStatements() {
        if (!Argument.IsPartOfDeclaration)
            return [];

        return [$"GC.KeepAlive({Argument.MarshalledArgument.Name});"];
    }
}

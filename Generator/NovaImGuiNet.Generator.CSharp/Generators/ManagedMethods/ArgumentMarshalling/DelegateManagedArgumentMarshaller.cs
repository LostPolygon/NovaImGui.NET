using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class DelegateManagedArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() {
        string value = Argument.MarshalledArgument.Name;
        if (!Argument.IsPartOfDeclaration) {
            Debug.Assert(Argument.MarshalledArgument.DefaultValue == null);
        }

        return [
            $"IntPtr {InvocationVariableName} = {value} != null ? Marshal.GetFunctionPointerForDelegate({value}) : default;"
        ];
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateUnmarshalStatements() {
        return [$"GC.KeepAlive({Argument.MarshalledArgument.Name});"];
    }
}

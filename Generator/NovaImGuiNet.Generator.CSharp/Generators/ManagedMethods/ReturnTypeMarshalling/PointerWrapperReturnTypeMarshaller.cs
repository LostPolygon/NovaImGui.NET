using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerWrapperReturnTypeMarshaller : PassthroughReturnTypeMarshaller {
    public PointerWrapperReturnTypeMarshaller(MethodSignature methodSignature, string nativeFunctionInvocation) : base(methodSignature, nativeFunctionInvocation) {
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateReturnStatements() {
        string returnTypeValue =
            FormatTypeDefault(
                MethodSignature.ReturnType.MarshalledReturnType.AsEntityReference().ExpandPointer().Value
            );

        if (CanSimplifyReturn()) {
            if (returnTypeValue == "Pointer<IntPtr>") {
                return [$"return new Pointer<IntPtr>((IntPtr*) {NativeFunctionInvocation});"];
            } else {
                returnTypeValue = FormatTypeDefault(MethodSignature.NativeFunction.ReturnType.Value);
                return [$"return ({returnTypeValue}) {NativeFunctionInvocation};"];
            }
        }

        Debug.Assert(!NativeFunction.IsConstructor);

        // We need to remove the pointer from the returned type
        if (returnTypeValue == "Pointer<IntPtr>") {
            return [$"return new Pointer<IntPtr>((IntPtr*) {ReturnVariableName});"];
        } else {
            return [$"return {ReturnVariableName};"];
        }
    }
}

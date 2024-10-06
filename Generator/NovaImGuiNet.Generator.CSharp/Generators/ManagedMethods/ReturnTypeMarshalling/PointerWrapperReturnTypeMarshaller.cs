using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerWrapperReturnTypeMarshaller : ReturnTypeMarshaller {
    public PointerWrapperReturnTypeMarshaller(MethodSignature methodSignature, string nativeFunctionInvocation) : base(methodSignature, nativeFunctionInvocation) {
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateInvokeStatements() {
        return [
            $"{NativeReturnType} {ReturnVariableName};",
            $"{ReturnVariableName} = {NativeFunctionInvocation};"
        ];
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateReturnStatements() {
        Debug.Assert(!NativeFunction.IsConstructor);

        // We need to remove the pointer from the returned type
        string returnTypeValue = FormatTypeDefault(
            MethodSignature.ReturnType.MarshalledReturnType.AsEntityReference().ExpandPointer().Value
        );

        if (returnTypeValue != "Pointer<IntPtr>") {
            return [$"return {ReturnVariableName};"];
        } else {
            return [$"return Unsafe.AsRef<{returnTypeValue}>({ReturnVariableName});"];
        }
    }
}

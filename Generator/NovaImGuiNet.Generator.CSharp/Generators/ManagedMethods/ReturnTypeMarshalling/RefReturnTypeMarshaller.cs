using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class RefReturnTypeMarshaller : ReturnTypeMarshaller {
    public RefReturnTypeMarshaller(MethodSignature methodSignature, string nativeFunctionInvocation) : base(methodSignature, nativeFunctionInvocation) {
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
        return [
            $"if ({ReturnVariableName} == null) {{\nCommonExceptions.ThrowReturnedNativePointerNull();\n}}",
            $"return ref Unsafe.AsRef<{returnTypeValue}>({ReturnVariableName});"
        ];
    }
}

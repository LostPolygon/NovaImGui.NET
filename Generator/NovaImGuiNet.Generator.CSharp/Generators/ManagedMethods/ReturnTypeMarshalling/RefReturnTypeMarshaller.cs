using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class RefReturnTypeMarshaller : ReturnTypeMarshaller {
    public RefReturnTypeMarshaller(MethodSignature methodSignature, string nativeFunctionInvocation) : base(methodSignature, nativeFunctionInvocation) {
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateInvokeStatements() {
        return [
            $"{NativeReturnType} {ReturnVariableName};",
            $"{ReturnVariableName} = ({NativeReturnType}) {NativeFunctionInvocation};"
        ];
    }

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateReturnStatements() {
        Debug.Assert(!NativeFunction.IsConstructor);

        List<ManagedMethodArgumentMarshallerStatement> result = [
            $"if ({ReturnVariableName} == null) {{\nInteropExceptions.ThrowReturnedNativePointerNull();\n}}"
        ];

        // We need to remove the pointer from the returned type
        string returnTypeValue = FormatTypeDefault(
            MethodSignature.ReturnType.MarshalledReturnType.AsEntityReference().ExpandPointer().Value
        );

        if (returnTypeValue != "IntPtr") {
            result.Add($"return ref *{ReturnVariableName};");
        } else {
            result.Add($"return ref Unsafe.AsRef<{returnTypeValue}>({ReturnVariableName});");
        }

        return result;
    }
}

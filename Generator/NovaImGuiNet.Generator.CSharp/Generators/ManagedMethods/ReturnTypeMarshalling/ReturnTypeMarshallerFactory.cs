namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public static class ReturnTypeMarshallerFactory {
    public static IReturnTypeMarshaller Create(MethodSignature methodSignature, string nativeFunctionInvocation) {
        switch (methodSignature.ReturnType.MarshallingVariant.ReturnTypeMarshallingMode) {
            case ReturnTypeMarshallingMode.Void:
                return new VoidReturnTypeMarshaller(methodSignature, nativeFunctionInvocation);
            case ReturnTypeMarshallingMode.Passthrough:
                return new PassthroughReturnTypeMarshaller(methodSignature, nativeFunctionInvocation);
            case ReturnTypeMarshallingMode.NullTerminatedString:
                return new NullTerminatedStringReturnTypeMarshaller(methodSignature, nativeFunctionInvocation);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentException(
            $"Cannot create return type marshaller for type '{CSharpCodeGenerationUtility.FormatType(methodSignature.ReturnType.NativeReturnType)}' " +
            $"of native function '{methodSignature.NativeFunction.Namespace}.{methodSignature.NativeFunction.Name}' " +
            $"(marshalling variant: '{methodSignature.ReturnType.MarshallingVariant.ToString()}')",
            nameof(methodSignature)
        );
    }
}

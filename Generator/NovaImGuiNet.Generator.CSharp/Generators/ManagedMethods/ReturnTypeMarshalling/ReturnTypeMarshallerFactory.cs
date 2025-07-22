namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public static class ReturnTypeMarshallerFactory {
    public static IReturnTypeMarshaller Create(MethodSignature methodSignature, string nativeFunctionInvocation) {
        return methodSignature.ReturnType.MarshallingVariant.ReturnTypeMarshallingMode switch {
            ReturnTypeMarshallingMode.Void =>
                new VoidReturnTypeMarshaller(methodSignature, nativeFunctionInvocation),
            ReturnTypeMarshallingMode.Passthrough =>
                new PassthroughReturnTypeMarshaller(methodSignature, nativeFunctionInvocation),
            ReturnTypeMarshallingMode.PointerWrapper =>
                new PointerWrapperReturnTypeMarshaller(methodSignature, nativeFunctionInvocation),
            ReturnTypeMarshallingMode.NullTerminatedUtf8String =>
                new NullTerminatedUtf8StringReturnTypeMarshaller(methodSignature, nativeFunctionInvocation),
            ReturnTypeMarshallingMode.Ref =>
                new RefReturnTypeMarshaller(methodSignature, nativeFunctionInvocation),
            _ => throw new ArgumentException(
                $"Cannot create return type marshaller for type " +
                $"'{CSharpCodeGenerationUtility.FormatType(methodSignature.ReturnType.NativeReturnType)}' " +
                $"of native function '{methodSignature.NativeFunction.Namespace}.{methodSignature.NativeFunction.Name}' " +
                $"(marshalling variant: '{methodSignature.ReturnType.MarshallingVariant.ToString()}')",
                nameof(methodSignature)
            )
        };
    }
}

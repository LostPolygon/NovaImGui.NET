namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static ArgumentMarshallingMode;

public static class ArgumentMarshallerFactory {
    public static IArgumentMarshaller Create(MethodSignature methodSignature, ManagedMethodArgument argument) {
        if (argument.NativeArgument.IsInstancePointer)
            return new InstanceThisArgumentMarshaller(methodSignature, argument);

        return argument.MarshallingVariant.ArgumentMarshallingMode switch {
            Passthrough => new PassthroughArgumentMarshaller(methodSignature, argument),
            StringBytePointer => new StringBytePointerArgumentMarshaller(methodSignature, argument),
            StringReadOnlyByteSpan => new StringReadOnlyByteSpanArgumentMarshaller(methodSignature, argument),
            StringReadOnlyCharSpan or StringManaged => new StringReadOnlyCharSpanArgumentMarshaller(methodSignature, argument),
            StringImGuiUtf8StringWriter => new StringImGuiUtf8StringWriterArgumentMarshaller(methodSignature, argument),
            Ref => new PointerRefArgumentMarshaller(methodSignature, argument),
            Out => new PointerOutArgumentMarshaller(methodSignature, argument),
            IntPtr => new PointerIntPtrArgumentMarshaller(methodSignature, argument),
            IntPtrRef => new PointerIntPtrRefArgumentMarshaller(methodSignature, argument),
            IntPtrOut => new PointerIntPtrOutArgumentMarshaller(methodSignature, argument),
            DelegateManaged => new DelegateManagedArgumentMarshaller(methodSignature, argument),
            DelegateFunctionPointer => new DelegateFunctionPointerArgumentMarshaller(methodSignature, argument),
            DelegateIntPtr => new PassthroughArgumentMarshaller(methodSignature, argument),
            _ => throw new ArgumentException(
                $"Cannot create argument marshaller for argument '{argument.NativeArgument.Name}' of native function " +
                $"'{methodSignature.NativeFunction.Namespace}.{methodSignature.NativeFunction.Name}' " +
                $"(marshalling variant: '{argument.MarshallingVariant}')",
                nameof(argument)
            )
        };
    }
}

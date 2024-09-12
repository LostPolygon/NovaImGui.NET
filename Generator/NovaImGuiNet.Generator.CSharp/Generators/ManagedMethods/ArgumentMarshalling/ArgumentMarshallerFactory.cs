namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public static class ArgumentMarshallerFactory {
    public static IArgumentMarshaller Create(MethodSignature methodSignature, ManagedMethodArgument argument) {
        if (argument.NativeArgument.IsInstancePointer) {
            return new InstanceThisArgumentMarshaller(methodSignature, argument);
        }

        switch (argument.MarshallingVariant.StringMarshallingMode) {
            case StringMarshallingMode.BytePointer:
                break;
            case StringMarshallingMode.ReadOnlyByteSpan:
                return new StringReadOnlyByteSpanArgumentMarshaller(methodSignature, argument);
            case StringMarshallingMode.ReadOnlyCharSpan:
            case StringMarshallingMode.String:
                return new StringReadOnlyCharSpanArgumentMarshaller(methodSignature, argument);
            case StringMarshallingMode.Undefined:
            default:
                break;
        }

        switch (argument.MarshallingVariant.PointerMarshallingMode) {
            case PointerMarshallingMode.Pointer:
                return new PassthroughArgumentMarshaller(methodSignature, argument);
            case PointerMarshallingMode.Ref:
                return new PointerRefArgumentMarshaller(methodSignature, argument);
            case PointerMarshallingMode.Out:
                return new PointerOutArgumentMarshaller(methodSignature, argument);
            case PointerMarshallingMode.IntPtr:
                return new PointerIntPtrArgumentMarshaller(methodSignature, argument);
            case PointerMarshallingMode.IntPtrRef:
                return new PointerIntPtrRefArgumentMarshaller(methodSignature, argument);
            case PointerMarshallingMode.IntPtrOut:
                break;
            case PointerMarshallingMode.Undefined:
            default:
                break;
        }

        if (true || argument.MarshallingVariant.StringMarshallingMode == StringMarshallingMode.Undefined &&
            argument.MarshallingVariant.PointerMarshallingMode == PointerMarshallingMode.Undefined) {
            return new PassthroughArgumentMarshaller(methodSignature, argument);
        }

        throw new ArgumentException(
            $"Cannot create argument marshaller for argument '{argument.NativeArgument.Name}' " +
            $"of native function '{methodSignature.NativeFunction.Namespace}.{methodSignature.NativeFunction.Name}' " +
            $"(marshalling variant: '{argument.MarshallingVariant.ToString()}')",
            nameof(argument)
        );
    }
}

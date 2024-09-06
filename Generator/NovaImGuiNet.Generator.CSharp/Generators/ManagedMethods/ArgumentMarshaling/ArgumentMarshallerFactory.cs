namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public static class ArgumentMarshallerFactory {
    public static IArgumentMarshaller Create(MethodSignature methodSignature, ManagedMethodArgument argument) {
        if (argument.NativeArgument.IsInstancePointer) {
            return new InstanceThisArgumentMarshaller(methodSignature, argument);
        }

        switch (argument.MarshalingVariant.StringMarshalingMode) {
            case StringMarshalingMode.BytePointer:
                break;
            case StringMarshalingMode.ReadOnlyByteSpan:
                return new StringReadOnlyByteSpanArgumentMarshaller(methodSignature, argument);
            case StringMarshalingMode.ReadOnlyCharSpan:
            case StringMarshalingMode.String:
                return new StringReadOnlyCharSpanArgumentMarshaller(methodSignature, argument);
            case StringMarshalingMode.Undefined:
            default:
                break;
        }

        switch (argument.MarshalingVariant.PointerMarshalMode) {
            case PointerMarshalMode.Pointer:
                return new PassthroughArgumentMarshaller(methodSignature, argument);
            case PointerMarshalMode.Ref:
                return new PointerRefArgumentMarshaller(methodSignature, argument);
            case PointerMarshalMode.Out:
                return new PointerOutArgumentMarshaller(methodSignature, argument);
            case PointerMarshalMode.IntPtr:
                return new PointerIntPtrArgumentMarshaller(methodSignature, argument);
            case PointerMarshalMode.IntPtrRef:
                return new PointerIntPtrRefArgumentMarshaller(methodSignature, argument);
            case PointerMarshalMode.IntPtrOut:
                break;
            case PointerMarshalMode.Undefined:
            default:
                break;
        }

        if (true || argument.MarshalingVariant.StringMarshalingMode == StringMarshalingMode.Undefined &&
            argument.MarshalingVariant.PointerMarshalMode == PointerMarshalMode.Undefined) {
            return new PassthroughArgumentMarshaller(methodSignature, argument);
        }

        throw new ArgumentException(
            $"Cannot create argument marshaller for argument '{argument.NativeArgument.Name}' " +
            $"of native function '{methodSignature.NativeFunction.Namespace}.{methodSignature.NativeFunction.Name}' " +
            $"(marshaling variant: '{argument.MarshalingVariant.ToString()}')",
            nameof(argument)
        );
    }
}

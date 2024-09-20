namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerIntPtrOutArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() =>
        new(
            InvocationVariable,
            [
                $"void* {InternalVar("_out_native")};",
                $"void** {InvocationVariable} = &{InternalVar("_out_native")};"
            ],
            [$"{Argument.MarshalledArgument.Name} = ({MarshalledArgumentType}) (*{InvocationVariable});"]
        );
}

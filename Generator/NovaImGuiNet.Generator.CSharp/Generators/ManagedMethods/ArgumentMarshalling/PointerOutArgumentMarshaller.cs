//#define USE_UNSAFE_AS_POINTER

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerOutArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    protected override string InvocationVariable => InternalVar("_out_native_ptr");

#if USE_UNSAFE_AS_POINTER
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() => [
        $"Unsafe.SkipInit(out {Argument.MarshalledArgument.Name});",
        $"{MarshalledArgumentType}* {DefaultInvocationExpression} = " +
        $"({MarshalledArgumentType}*) Unsafe.AsPointer(ref {Argument.MarshalledArgument.Name});"
    ];
#endif

#if !USE_UNSAFE_AS_POINTER
    public override ArgumentMarshallerOutput Marshall() =>
        new(
            InvocationVariable,
            [
                $"{MarshalledArgumentType} {InternalVar("_out_native")};",
                $"{MarshalledArgumentType}* {InvocationVariable} = &{InternalVar("_out_native")};",
            ],
            [$"{Argument.MarshalledArgument.Name} = *{InvocationVariable};"]
        );
#endif
}

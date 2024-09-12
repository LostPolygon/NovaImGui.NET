//#define USE_UNSAFE_AS_POINTER

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerOutArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    protected override string CreateInvocationVariableName() {
        return InternalVar("_out_native_ptr");
    }

#if USE_UNSAFE_AS_POINTER
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() => [
        $"Unsafe.SkipInit(out {Argument.MarshalledArgument.Name});",
        $"{MarshalledArgumentType}* {InvocationVariableName} = " +
        $"({MarshalledArgumentType}*) Unsafe.AsPointer(ref {Argument.MarshalledArgument.Name});"
    ];
#endif

#if !USE_UNSAFE_AS_POINTER
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() => [
        $"{MarshalledArgumentType} {InternalVar("_out_native")};",
        $"{MarshalledArgumentType}* {InvocationVariableName} = &{InternalVar("_out_native")};",
    ];

    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateUnmarshalStatements() =>
        [$"{Argument.MarshalledArgument.Name} = *{InvocationVariableName};"];
#endif
}

//#define USE_UNSAFE_AS_POINTER

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class PointerOutArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    protected override string InvocationVariable => InternalVar("_out_native");

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
            $"(IntPtr) (&{InvocationVariable})",
            [
                // TODO: ImFontGlyphRangesBuilder.BuildRanges crashes if ranges are not zero-initialized,
                // but check if there's an automated way to skip the zero-initialization
                $"{MarshalledArgumentType} {InternalVar("_out_native")} = default;",
            ],
            [$"{Argument.MarshalledArgument.Name} = {InvocationVariable};"]
        );
#endif
}

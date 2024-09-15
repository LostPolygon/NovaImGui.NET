namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public sealed record MethodSignatureDiscriminator {
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    private ReturnTypeDiscriminator ReturnType { get; }
    private EqualityList<ArgumentDiscriminator> Arguments { get; }
    // ReSharper restore UnusedAutoPropertyAccessor.Local

    private MethodSignatureDiscriminator(
        ReturnTypeDiscriminator ReturnType,
        EqualityList<ArgumentDiscriminator> Arguments
    ) {
        this.ReturnType = ReturnType;
        this.Arguments = Arguments;
    }

    public static MethodSignatureDiscriminator Create(MethodSignature methodSignature) {
        EqualityList<ArgumentDiscriminator> arguments = [];
        foreach (ManagedMethodArgument argument in methodSignature.Arguments) {
            if (argument.IsPartOfDeclaration) {
                arguments.Add(
                    new ArgumentDiscriminator(
                        true,
                        argument.MarshallingVariant.ArgumentMarshallingMode,
                        argument.MarshallingVariant.StringBeginEndPairMode
                    )
                );
            } else {
                arguments.Add(
                    new ArgumentDiscriminator(
                        false,
                        ArgumentMarshallingMode.Passthrough,
                        StringBeginEndPairMode.None
                    )
                );
            }
        }

        return new MethodSignatureDiscriminator(
            new ReturnTypeDiscriminator(methodSignature.ReturnType.MarshallingVariant.ReturnTypeMarshallingMode),
            arguments
        );
    }

    private record struct ReturnTypeDiscriminator(
        ReturnTypeMarshallingMode ReturnTypeMarshallingMode
    );

    private record struct ArgumentDiscriminator(
        bool IsPartOfDeclaration,
        ArgumentMarshallingMode ArgumentMarshallingMode,
        StringBeginEndPairMode StringBeginEndPairMode
    );
}

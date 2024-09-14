namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public record MethodSignatureArgumentsDiscriminator(EqualityList<MethodSignatureArgumentsDiscriminator.ArgumentDiscriminator> Arguments) {
    public static MethodSignatureArgumentsDiscriminator Create(MethodSignature methodSignature) {
        EqualityList<ArgumentDiscriminator> list = [];
        foreach (ManagedMethodArgument argument in methodSignature.Arguments) {
            if (argument.IsPartOfDeclaration) {
                list.Add(
                    new ArgumentDiscriminator(
                        true,
                        argument.MarshallingVariant.ArgumentMarshallingMode,
                        argument.MarshallingVariant.StringBeginEndPairMode
                    )
                );
            } else {
                list.Add(new ArgumentDiscriminator(false, ArgumentMarshallingMode.Passthrough, StringBeginEndPairMode.None));
            }
        }

        return new MethodSignatureArgumentsDiscriminator(list);
    }

    public record struct ArgumentDiscriminator(
        bool IsPartOfDeclaration,
        ArgumentMarshallingMode ArgumentMarshallingMode,
        StringBeginEndPairMode StringBeginEndPairMode
    );
}

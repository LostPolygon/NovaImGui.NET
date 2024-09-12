namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public record MethodSignatureArgumentsDiscriminator(EqualityList<MethodSignatureArgumentsDiscriminator.ArgumentDiscriminator> Arguments) {
    public static MethodSignatureArgumentsDiscriminator Create(MethodSignature methodSignature) {
        EqualityList<ArgumentDiscriminator> list = [];
        foreach (ManagedMethodArgument argument in methodSignature.Arguments) {
            if (argument.IsPartOfDeclaration) {
                list.Add(
                    new ArgumentDiscriminator(
                        true,
                        argument.MarshallingVariant.StringMarshallingMode,
                        argument.MarshallingVariant.PointerMarshallingMode
                    )
                );
            } else {
                list.Add(new ArgumentDiscriminator(false, StringMarshallingMode.Undefined, PointerMarshallingMode.Undefined));
                list.Add(new ArgumentDiscriminator(false, StringMarshallingMode.Undefined, PointerMarshallingMode.Undefined));
            }
        }

        return new MethodSignatureArgumentsDiscriminator(list);
    }

    public record struct ArgumentDiscriminator(
        bool IsPartOfDeclaration,
        StringMarshallingMode StringMarshallingMode,
        PointerMarshallingMode PointerMarshallingMode
    );
}

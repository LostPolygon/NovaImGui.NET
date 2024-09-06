namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public record MethodSignatureArgumentsDiscriminator(EqualityList<MethodSignatureArgumentsDiscriminator.ArgumentDiscriminator> Arguments) {
    public static MethodSignatureArgumentsDiscriminator Create(MethodSignature methodSignature) {
        EqualityList<ArgumentDiscriminator> list = [];
        foreach (ManagedMethodArgument argument in methodSignature.Arguments) {
            if (argument.IsPartOfDeclaration) {
                list.Add(
                    new ArgumentDiscriminator(
                        true,
                        argument.MarshalingVariant.StringMarshalingMode,
                        argument.MarshalingVariant.PointerMarshalMode
                    )
                );
            } else {
                list.Add(new ArgumentDiscriminator(false, StringMarshalingMode.Undefined, PointerMarshalMode.Undefined));
                list.Add(new ArgumentDiscriminator(false, StringMarshalingMode.Undefined, PointerMarshalMode.Undefined));
            }
        }

        return new MethodSignatureArgumentsDiscriminator(list);
    }

    public record struct ArgumentDiscriminator(
        bool IsPartOfDeclaration,
        StringMarshalingMode StringMarshalingMode,
        PointerMarshalMode PointerMarshalMode
    );
}

using System.Diagnostics;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public class DelegateManagedArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override ArgumentMarshallerOutput Marshall() {
        string value = Argument.MarshalledArgument.Name;
        if (!Argument.IsPartOfDeclaration) {
            Debug.Assert(Argument.MarshalledArgument.DefaultValue != null);

            string defaultValue = Argument.MarshalledArgument.DefaultValue;
            if (KnownEntities.NullishValues.Contains(defaultValue)) {
                return new ArgumentMarshallerOutput(
                    "(IntPtr) default",
                    []
                );
            }

            if (defaultValue != null) {
                value = defaultValue;
            }
        }

        return new ArgumentMarshallerOutput(
            InvocationVariable,
            [$"IntPtr {InvocationVariable} = {value} != null ? Marshal.GetFunctionPointerForDelegate({value}) : default;"],
            Argument.IsPartOfDeclaration ? [$"GC.KeepAlive({Argument.MarshalledArgument.Name});"] : []
        );
    }
}

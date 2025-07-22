namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

public abstract class ReturnTypeMarshaller : IReturnTypeMarshaller  {
    public MethodSignature MethodSignature { get; }
    public string ReturnVariableName { get; }
    public string NativeFunctionInvocation { get; }

    public BindingsModel.Struct? NativeStruct => MethodSignature.NativeFunction.OwnerStruct?.Value;
    public BindingsModel.Function NativeFunction => MethodSignature.NativeFunction;
    public string NativeReturnType { get; }
    protected string MarshalledReturnType { get; }

    protected ReturnTypeMarshaller(MethodSignature methodSignature, string nativeFunctionInvocation) {
        MethodSignature = methodSignature;
        NativeFunctionInvocation = nativeFunctionInvocation;

        MarshalledReturnType = FormatTypeDefault(MethodSignature.ReturnType.MarshalledReturnType);
        NativeReturnType = FormatTypeDefault(MethodSignature.ReturnType.NativeReturnType);
        ReturnVariableName = CreateReturnVariableName();
    }

    public abstract IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateDeclarationStatements();

    public abstract IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateInvokeStatements();

    public virtual IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateReturnStatements() => [];

    protected virtual string CreateReturnVariableName() {
        return InternalVar("value");
    }

    protected virtual string InternalVar(string suffix) => "ret_" + suffix;

    protected bool CanSimplifyReturn() {
        return
            !NativeFunction.IsConstructor &&
            NativeStruct == null &&
            (MethodSignature.Arguments.Count == 0 ||
                MethodSignature.Arguments.All(arg =>
                    arg.MarshallingVariant.ArgumentMarshallingMode is
                        ArgumentMarshallingMode.Passthrough or
                        ArgumentMarshallingMode.IntPtr or
                        ArgumentMarshallingMode.StringBytePointer or
                        ArgumentMarshallingMode.DelegateFunctionPointer or
                        ArgumentMarshallingMode.DelegateIntPtr
                )
            );
    }

    protected static string FormatTypeDefault(BindingsModel.IEntity entity) {
        CSharpCodeGenerationUtility.FormatTypeOptions varTypeFormatOptions = new() {
            ExpandTypedefs = false,
        };

        return CSharpCodeGenerationUtility.FormatType(entity, varTypeFormatOptions);
    }
}

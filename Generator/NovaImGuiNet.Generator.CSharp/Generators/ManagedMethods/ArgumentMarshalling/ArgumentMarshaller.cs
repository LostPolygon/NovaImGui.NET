namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static BindingsModel;
using static CSharpCodeGenerationUtility;

public abstract class ArgumentMarshaller : IArgumentMarshaller {
    public MethodSignature MethodSignature { get; }
    public ManagedMethodArgument Argument { get; }

    public Struct? NativeStruct => MethodSignature.NativeFunction.OwnerStruct?.Value;
    public Function NativeFunction => MethodSignature.NativeFunction;
    public string InvocationVariableName { get; }
    public string NativeArgumentType { get; }

    protected string MarshalledArgumentType { get; }

    protected ArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument) {
        MethodSignature = methodSignature;
        Argument = argument;

        MarshalledArgumentType = FormatTypeDefault(Argument.MarshalledArgument.Type.Value);
        InvocationVariableName = CreateInvocationVariableName();
        NativeArgumentType = FormatTypeDefault(Argument.NativeArgument.Type.Value);
    }

    public abstract IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements();
    public virtual IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateUnmarshalStatements() => [];

    protected virtual string CreateInvocationVariableName() {
        return InternalVar("_in_native");
    }

    protected virtual string InternalVar(string suffix) => Argument.NativeArgument.Name + suffix;

    protected static string FormatTypeDefault(IEntity entity) {
        FormatTypeOptions varTypeFormatOptions = new() {
            ExpandTypedefs = false,
        };

        return FormatType(entity, varTypeFormatOptions);
    }
}

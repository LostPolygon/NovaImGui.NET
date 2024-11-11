using System.Runtime.CompilerServices;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static BindingsModel;
using static CSharpCodeGenerationUtility;

public abstract class ArgumentMarshaller : IArgumentMarshaller {
    public MethodSignature MethodSignature { get; }
    public ManagedMethodArgument Argument { get; }

    protected Struct? NativeStruct => MethodSignature.NativeFunction.OwnerStruct?.Value;
    protected Function NativeFunction => MethodSignature.NativeFunction;
    protected string NativeArgumentType { get; }

    protected string MarshalledArgumentType { get; }

    protected ArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument) {
        MethodSignature = methodSignature;
        Argument = argument;

        MarshalledArgumentType = FormatTypeDefault(Argument.MarshalledArgument.Type.Value);
        NativeArgumentType = FormatTypeDefault(Argument.NativeArgument.Type.Value);
    }

    public abstract ArgumentMarshallerOutput Marshall();

    protected virtual string InvocationVariable => InternalVar("_in_native");

    protected string InternalVar(string suffix) => Argument.NativeArgument.Name + suffix;

    protected string CastPointerToIntPtr() {
        return CastPointerToIntPtr(Argument.MarshalledArgument.Type.Value);
    }

    protected static string FormatTypeDefault(IEntity entity) {
        FormatTypeOptions varTypeFormatOptions = new() {
            ExpandTypedefs = false,
        };

        return FormatType(entity, varTypeFormatOptions);
    }

    protected static string RequireNonNull(
        string? text,
        [CallerArgumentExpression(nameof(text))] string callerArgumentExpressionAttribute = ""
    ) {
        if (String.IsNullOrWhiteSpace(text))
            throw new ArgumentException(callerArgumentExpressionAttribute, nameof(text));

        return text;
    }

    protected static string CastPointerToIntPtr(Entity entity) {
        if (entity is not Pointer pointer)
            return "";

        return "(IntPtr) ";
    }
}

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static BindingsModel;
using static CSharpCodeGenerationUtility;
using static SyntaxFactory;
using static SyntaxFactoryCached;

public abstract class ArgumentMarshaller : IArgumentMarshaller {
    public MethodSignature MethodSignature { get; }
    public ManagedMethodArgument Argument { get; }

    protected Struct? NativeStruct => MethodSignature.NativeFunction.OwnerStruct?.Value;
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

    protected static IReadOnlyList<ManagedMethodArgumentMarshallerStatement> StartFixedBlock(
        string variableTypeName,
        string variableIdentifier,
        string variableValueExpression
    ) => [
        ArgumentMarshallerCommand.BlockStart,
        FixedStatement(
            VariableDeclaration(
                ParseTypeName(variableTypeName),
                SeparatedList([
                    VariableDeclarator(
                        Identifier(variableIdentifier),
                        null,
                        EqualsValueClause(ParseExpression(variableValueExpression))
                    )
                ])
            ),
            EmptyStatement()
        )
    ];
}

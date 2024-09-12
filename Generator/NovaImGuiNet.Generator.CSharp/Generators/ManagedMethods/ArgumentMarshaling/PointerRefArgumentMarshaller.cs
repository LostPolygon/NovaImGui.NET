#define USE_UNSAFE_AS_POINTER

#if !USE_UNSAFE_AS_POINTER
using Microsoft.CodeAnalysis.CSharp;
#endif

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

#if !USE_UNSAFE_AS_POINTER
using static SyntaxFactory;
using static SyntaxFactoryCached;
#endif

public class PointerRefArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
#if USE_UNSAFE_AS_POINTER
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() => [
        $"{NativeArgumentType}* {InvocationVariableName} = " +
        $"({NativeArgumentType}*) Unsafe.AsPointer(ref {Argument.MarshaledArgument.Name});"
    ];
#endif

#if !USE_UNSAFE_AS_POINTER
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() => [
        $"{MarshaledArgumentType}* {InvocationVariableName};",
        FixedStatement(
            VariableDeclaration(
                ParseTypeName($"{MarshaledArgumentType}*"),
                SeparatedList([
                    VariableDeclarator(
                        Identifier($"{InvocationVariableName}_tmp"),
                        null,
                        EqualsValueClause(ParseExpression($"&{Argument.MarshaledArgument.Name}"))
                    )
                ])
            ),
            ParseStatementWithLeadingLineFeed($"{InvocationVariableName} = {InvocationVariableName}_tmp;")
        )
    ];
#endif
}

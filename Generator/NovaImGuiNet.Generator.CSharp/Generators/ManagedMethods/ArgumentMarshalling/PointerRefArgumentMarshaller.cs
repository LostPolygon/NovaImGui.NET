//#define USE_UNSAFE_AS_POINTER

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
        $"{MarshalledArgumentType}* {DefaultInvocationExpression} = " +
        $"({MarshalledArgumentType}*) Unsafe.AsPointer(ref {Argument.MarshalledArgument.Name});"
    ];
#endif

#if !USE_UNSAFE_AS_POINTER
    public override ArgumentMarshallerOutput Marshall() =>
        new(
            InvocationVariable,
            [
                $"{MarshalledArgumentType}* {InvocationVariable};",
                FixedStatement(
                    VariableDeclaration(
                        ParseTypeName($"{MarshalledArgumentType}*"),
                        SeparatedList([
                            VariableDeclarator(
                                Identifier($"{InvocationVariable}_tmp"),
                                null,
                                EqualsValueClause(ParseExpression($"&{Argument.MarshalledArgument.Name}"))
                            )
                        ])
                    ),
                    ParseStatementWithLeadingLineFeed($"{InvocationVariable} = {InvocationVariable}_tmp;")
                )
            ]
        );
#endif
}

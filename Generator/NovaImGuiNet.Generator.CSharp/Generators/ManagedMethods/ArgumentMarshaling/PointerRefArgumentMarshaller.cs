using Microsoft.CodeAnalysis.CSharp;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static SyntaxFactory;

public class PointerRefArgumentMarshaller(MethodSignature methodSignature, ManagedMethodArgument argument)
    : ArgumentMarshaller(methodSignature, argument) {
    public override IReadOnlyList<ManagedMethodArgumentMarshallerStatement> CreateMarshalStatements() {
        const bool useUnsafeAsPointer = false;
        if (useUnsafeAsPointer) {
            return [
                $"{NativeArgumentType}* {InvocationVariableName} = " +
                $"({NativeArgumentType}*) Unsafe.AsPointer(ref {Argument.MarshaledArgument.Name});",
            ];
        }

        return [
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
                ParseStatement($"{InvocationVariableName} = {InvocationVariableName}_tmp;")
            )
        ];
    }
}

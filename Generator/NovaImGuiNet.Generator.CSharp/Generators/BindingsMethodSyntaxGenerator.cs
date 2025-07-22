using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OneOf;

namespace NovaImGuiNet.Generator.CSharp;

using static CSharpCodeGenerationUtility;
using static BindingsModel;
using static SyntaxFactory;
using static SyntaxFactoryCached;
using static BindingsGenerationUtility;

public static class BindingsMethodSyntaxGenerator {
    public static BaseMethodDeclarationSyntax GenerateFunctionBindingMethodSyntax(
        Function function,
        bool isStaticMethod,
        bool isNativeMethod,
        bool delegatesAsFunctionPointers,
        OneOf<IReadOnlyList<StatementSyntax>, ArrowExpressionClauseSyntax>? bodySyntax
    ) {
        List<SyntaxKind> modifiers = [SyntaxKind.PublicKeyword];

        if (isStaticMethod) {
            modifiers.Add(SyntaxKind.StaticKeyword);
        }

        BaseMethodDeclarationSyntax methodDeclarationSyntax;
        if (function.IsConstructor && !isNativeMethod) {
            methodDeclarationSyntax =
                ConstructorDeclaration(FormatType(function.OwnerStruct!.Value))
                    .AddModifiers(modifiers.Select(Token).ToArray());
        } else {
            // Ugliness for ref return support
            TypeSyntax returnTypeSyntax;
            if (function.ReturnType.Value is Pointer { PointerType: BindingsModel.PointerType.ManagedRef } pointer) {
                returnTypeSyntax = RefType(ParseTypeName(FormatType(pointer.ValueType.Value)));
            } else {
                returnTypeSyntax = ParseTypeName(FormatType(function.ReturnType.Value));
            }

            methodDeclarationSyntax =
                MethodDeclaration(returnTypeSyntax, function.Name)
                    .AddModifiers(modifiers.Select(Token).ToArray());
        }

        if (isNativeMethod) {
            methodDeclarationSyntax =
                methodDeclarationSyntax
                    .AddAttributeLists(CreateMarshalAsAttributeLists(function.ReturnType.Value, true));
        }

        if (bodySyntax != null) {
            BaseMethodDeclarationSyntax methodDeclarationSyntaxCopy = methodDeclarationSyntax;
            methodDeclarationSyntax =
                bodySyntax.Value.Match(
                    statementSyntaxes => {
                        /*if (statementSyntaxes.Count == 1 && statementSyntaxes[0] is not FixedStatementSyntax) {
                            ExpressionSyntax? expressionSyntax = statementSyntaxes[0] switch {
                                ExpressionStatementSyntax expressionStatementSyntax => expressionStatementSyntax.Expression,
                                ReturnStatementSyntax returnStatementSyntax => returnStatementSyntax.Expression,
                                _ => null
                            };

                            if (expressionSyntax != null) {
                                return methodDeclarationSyntaxCopy
                                    .WithExpressionBody(
                                        ArrowExpressionClause(
                                            Token(
                                                TriviaList(),
                                                SyntaxKind.EqualsGreaterThanToken,
                                                TriviaList(Space)
                                            ),
                                            expressionSyntax.WithLeadingTrivia(LineFeed, Whitespace("    "))
                                        )
                                    )
                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                            }
                        }*/

                        return methodDeclarationSyntaxCopy.WithBody(Block(statementSyntaxes));
                    },
                    arrowExpressionSyntax =>
                        methodDeclarationSyntaxCopy
                            .WithExpressionBody(arrowExpressionSyntax)
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
                );
        } else {
            methodDeclarationSyntax = methodDeclarationSyntax.WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        IEnumerable<ParameterSyntax> parameters =
            function.Arguments.Select(arg =>
                CreateMethodParameterSyntax(arg, delegatesAsFunctionPointers, isNativeMethod)
            );

        methodDeclarationSyntax =
            methodDeclarationSyntax.WithParameterList(ParameterList(SeparatedList(parameters)));

        return methodDeclarationSyntax;
    }

    public static ParameterSyntax CreateMethodParameterSyntax(
        FunctionArgument argument,
        bool delegatesAsFunctionPointers,
        bool addMarshalling
    ) {
        bool isDelegate = argument.Type.ExpandAllTypedefsAndPointers<Delegate>() != null;

        ParameterSyntax parameter =
            Parameter(Identifier(argument.Name))
                .WithType(
                    ParseTypeName(
                        FormatType(
                            argument.Type.Value,
                            new FormatTypeOptions {
                                DelegatesAsFunctionPointers = delegatesAsFunctionPointers && isDelegate,
                                ExpandTypedefs = delegatesAsFunctionPointers,
                            }
                        )
                    )
                );

        if (addMarshalling) {
            parameter =
                parameter
                    .AddAttributeLists(CreateMarshalAsAttributeLists(argument.Type.Value, false));
        }

        if (argument.PassType != FunctionArgumentPassType.Normal) {
            parameter = parameter.AddModifiers(
                Token(
                    argument.PassType switch {
                        FunctionArgumentPassType.Ref => SyntaxKind.RefKeyword,
                        FunctionArgumentPassType.Out => SyntaxKind.OutKeyword,
                        FunctionArgumentPassType.In => SyntaxKind.InKeyword,
                        _ => throw new ArgumentOutOfRangeException()
                    }
                )
            );
        }

        if (argument.DefaultValue != null) {
            if (argument.IsDefaultValueValidInDeclaration) {
                parameter = parameter.WithDefault(EqualsValueClause(ParseExpression(argument.DefaultValue)));
            } else {
                parameter = parameter.AddAttributeLists(
                    SimpleAttributeList(
                        "ImGuiDefaultValue",
                        LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(argument.DefaultValue))
                    )
                );
            }
        }

        if (argument.Attributes.Count != 0) {
            parameter = parameter.AddAttributeLists(AttributeList(SeparatedList(argument.Attributes)));
        }

        return parameter;
    }

    public static AttributeListSyntax[] CreateMarshalAsAttributeLists(Entity entity, bool isReturnValue) {
        if (entity.AsEntityReference().ExpandTypedef().Value is Builtin builtin &&
            builtin.ManagedType == typeof(bool)) {
            return [CreateBoolMarshalAsAttribute(isReturnValue ? SyntaxKind.ReturnKeyword : null)];
        }

        return [];
    }

    private static AttributeListSyntax CreateBoolMarshalAsAttribute(SyntaxKind? target) =>
        AttributeList(
            target != null ? AttributeTargetSpecifier(Token(target.Value)) : null,
            SeparatedList(
                [
                    SimpleAttribute("MarshalAs", "UnmanagedType.I1")
                ]
            )
        );
}

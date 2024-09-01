using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OneOf;

namespace NovaImGuiNet.Generator.CSharp;

using static SyntaxFactory;
using static CSharpCodeGenerationUtility;
using static BindingsModel;

public class BindingsModelCodeGenerator {
    public BindingsModel BindingsModel { get; }
    public BindingsContext BindingsContext { get; }

    public BindingsModelCodeGenerator(
        BindingsModel metadataModel,
        BindingsContext bindingsContext
    ) {
        BindingsModel = metadataModel;
        BindingsContext = bindingsContext;
    }

    public IReadOnlySet<BindingsCompilationUnit> Generate() {
        HashSet<BindingsCompilationUnit> nameToCodeMap = [];

        void AddCompilationUnit(OneOf<ITopLevelEntity, string> entityOrName, CSharpSyntaxNode syntaxNode) {
            bool added = nameToCodeMap.Add(new BindingsCompilationUnit(entityOrName, syntaxNode));
            Debug.Assert(added);
        }

        (
            CompilationUnitSyntax globalUsingsCompilationUnit,
            CompilationUnitSyntax delegatesCompilationUnit
            ) typedefBindings = GenerateTypedefBindings();

        AddCompilationUnit("Typedefs.GlobalUsings", typedefBindings.globalUsingsCompilationUnit);
        AddCompilationUnit("Typedefs.Delegates", typedefBindings.delegatesCompilationUnit);

        foreach (EntityReferenceWrapper<Enum> enumeration in BindingsModel.Enums) {
            CompilationUnitSyntax compilationUnitSyntax = WrapRootLevelDefinition(
                enumeration.Value.Namespace,
                GenerateBinding(enumeration.Value)
            );

            AddCompilationUnit(enumeration.Value, compilationUnitSyntax);
        }

        foreach (EntityReferenceWrapper<Struct> structure in BindingsModel.Structs) {
            if (structure.Value.SkipEmit)
                continue;

            CompilationUnitSyntax compilationUnitSyntax = WrapRootLevelDefinition(
                structure.Value.Namespace,
                GenerateBinding(structure.Value)
            );

            AddCompilationUnit(structure.Value, compilationUnitSyntax);
        }

        CompilationUnitSyntax functionsCompilationUnit = GenerateNativeFunctionBindings();
        AddCompilationUnit("ImGuiNative", functionsCompilationUnit);

        return nameToCodeMap;
    }

    private CompilationUnitSyntax GenerateNativeFunctionBindings() {
        List<MemberDeclarationSyntax> functions = [];
        EntityReferenceWrapper<Function>[] allNativeFunctions =
            BindingsModel
                .GetAllFunctions()
                .Where(x => x.Value.IsNativeFunction)
                .ToArray();

        foreach (EntityReferenceWrapper<Function> functionReference in allNativeFunctions) {
            Function apiModelFunction = functionReference.Value;
            functions.AddRange(GenerateNativeFunctionBinding(apiModelFunction));
        }

        ClassDeclarationSyntax classDeclaration =
            ClassDeclaration(Identifier("ImGuiNative"))
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.UnsafeKeyword),
                    Token(SyntaxKind.PartialKeyword)
                )
                .AddMembers(
                    ParseMemberDeclaration("public const string ImGuiDllName = \"cimgui\";")!,
                    ParseMemberDeclaration("public const CallingConvention ImGuiCallingConvention = CallingConvention.Cdecl;")!
                )
                .AddMembers(functions.ToArray());

        CompilationUnitSyntax compilationUnitSyntax = WrapRootLevelDefinition(
            BindingsContext.Namespace,
            classDeclaration
        );

        return compilationUnitSyntax;
    }

    private(CompilationUnitSyntax globalUsingsCompilationUnit, CompilationUnitSyntax delegatesCompilationUnit) GenerateTypedefBindings() {
        (string @namespace, List<OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax>> typedefBindingsNamespace)[] typedefBindings =
            BindingsModel.Typedefs
                .Select(t => t.Value)
                .OrderBy(t => t.Type.Value is Delegate)
                .Where(t => !t.SkipEmit)
                .GroupBy(t => t.Namespace)
                .Select(t => {
                    List<OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax>> typedefBindingsNamespace = [];
                    foreach (Typedef typedefByNamespace in t) {
                        typedefBindingsNamespace.Add(GenerateBinding(typedefByNamespace));
                    }

                    return (t.Key, typedefBindingsNamespace);
                })
                .ToArray();

        CompilationUnitSyntax delegatesCompilationUnit =
            CompilationUnit()
                .AddMembers(CreateNamespaceDeclarationSyntax(BindingsContext.Namespace));
        CompilationUnitSyntax globalUsingsCompilationUnit = CompilationUnit();

        foreach ((string @namespace, List<OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax>> typedefs) in typedefBindings) {
            foreach (OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax> typedef in typedefs) {
                typedef.Switch(
                    delegateBinding => delegatesCompilationUnit = delegatesCompilationUnit.AddMembers(delegateBinding),
                    globalUsingBinding => globalUsingsCompilationUnit = globalUsingsCompilationUnit.AddUsings(globalUsingBinding)
                );
            }
        }

        return (globalUsingsCompilationUnit, delegatesCompilationUnit);
    }

    private IReadOnlyList<MethodDeclarationSyntax> GenerateNativeFunctionBinding(Function function) {
        IEntityReference[] allChildTypes = function.GetReferencedTypes([]).ToArray();
        IEntityReference[] callbacks = [
            ..allChildTypes.Where(x => x.Value is Delegate),
            ..allChildTypes.Where(t => t.Value is Typedef { Type.Value: Delegate })
        ];

        if (callbacks.Length != 0) {
            MethodDeclarationSyntax managedMethodSyntax = Generate(false);
            MethodDeclarationSyntax unmanagedMethodSyntax = Generate(true);
            unmanagedMethodSyntax = ApplyConditionals(
                unmanagedMethodSyntax,
                [new Conditional("NET6_0_OR_GREATER")]
            );

            return [managedMethodSyntax, unmanagedMethodSyntax];
        }

        return [Generate(false)];

        MethodDeclarationSyntax Generate(bool delegatesAsFunctionPointers) {
            bool useLibraryImport = true;

            List<AttributeSyntax> importAttributes;
            if (useLibraryImport) {
                importAttributes = [
                    Attribute(ParseName("LibraryImport"))
                        .WithArgumentList(AttributeArgumentList(
                            SeparatedList(
                                [
                                    AttributeArgument(ParseExpression("ImGuiDllName")),
                                    AttributeArgument(ParseExpression("StringMarshalling = StringMarshalling.Utf8"))
                                ]
                            )
                        )),
                    Attribute(ParseName("UnmanagedCallConv"))
                        .WithArgumentList(AttributeArgumentList(
                            SeparatedList(
                                [
                                    AttributeArgument(ParseExpression("CallConvs = [typeof(CallConvCdecl)]"))
                                ]
                            )
                        ))
                ];
            } else {
                importAttributes = [
                    Attribute(ParseName("DllImport"))
                        .WithArgumentList(AttributeArgumentList(
                            SeparatedList(
                                [
                                    AttributeArgument(ParseExpression("ImGuiDllName")),
                                    AttributeArgument(ParseExpression("CallingConvention = ImGuiCallingConvention"))
                                ]
                            )
                        ))
                ];
            }

            MethodDeclarationSyntax methodDeclarationSyntax =
                GenerateFunctionBinding(
                        function,
                        false,
                        true,
                        delegatesAsFunctionPointers,
                        null
                    )
                    .AddAttributeLists(
                        importAttributes
                            .Select(attr =>
                                AttributeList(SeparatedList([attr]))
                            )
                            .ToArray()
                    );

            List<string> generatorCommentItems = [$"OriginalFullyQualifiedName = {function.OriginalFullyQualifiedName}"];
            if (function.OwnerStruct != null) {
                generatorCommentItems.Add($"OriginalClass = {FormatType(function.OwnerStruct.Value)}");
            }

            if (function.IsUnformattedHelper) {
                generatorCommentItems.Add("IsUnformattedHelper");
            }

            string generatorComment = String.Join(", ", generatorCommentItems);
            SyntaxTrivia[] generatorTrivia = [
                Comment($"// {generatorComment}"),
                LineFeed,
                LineFeed
            ];

            methodDeclarationSyntax =
                methodDeclarationSyntax
                    .AddLeadingTriviaToEnd(generatorTrivia);

            methodDeclarationSyntax = ApplyCommonEntitySyntax(methodDeclarationSyntax, function);

            return methodDeclarationSyntax;
        }
    }

    private static MethodDeclarationSyntax GenerateFunctionBinding(
        Function function,
        bool isInstanceMethod,
        bool isNativeMethod,
        bool delegatesAsFunctionPointers,
        BlockSyntax? bodyBlockSyntax
    ) {
        List<SyntaxKind> modifiers = [SyntaxKind.PublicKeyword];

        if (!isInstanceMethod) {
            modifiers.Add(SyntaxKind.StaticKeyword);
        }

        if (isNativeMethod) {
            modifiers.Add(SyntaxKind.ExternKeyword);
        }

        MethodDeclarationSyntax methodDeclarationSyntax =
            MethodDeclaration(
                    ParseTypeName(FormatType(function.ReturnType.Value)),
                    function.Name
                )
                .AddModifiers(modifiers.Select(Token).ToArray());

        if (isNativeMethod) {
            methodDeclarationSyntax =
                methodDeclarationSyntax
                    .AddAttributeLists(CreateMarshalAsAttributeLists(function.ReturnType.Value, true));
        }

        if (bodyBlockSyntax != null) {
            methodDeclarationSyntax = methodDeclarationSyntax.WithBody(bodyBlockSyntax);
        } else {
            methodDeclarationSyntax = methodDeclarationSyntax.WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        IEnumerable<ParameterSyntax> parameters =
            function.Arguments.Select(arg => CreateMethodParameterSyntax(arg, delegatesAsFunctionPointers));

        methodDeclarationSyntax =
            methodDeclarationSyntax.WithParameterList(ParameterList(SeparatedList(parameters)));

        return methodDeclarationSyntax;
    }

    private static ParameterSyntax CreateMethodParameterSyntax(
        FunctionArgument argument,
        bool delegatesAsFunctionPointers
    ) {
        bool isDelegate = argument.Type.ExpandWrappers<Delegate>() != null;

        ParameterSyntax parameter =
            Parameter(Identifier(argument.Name))
                .WithType(
                    ParseTypeName(
                        FormatType(
                            argument.Type.Value,
                            new FormatTypeOptions {
                                DelegatesAsFunctionPointers = delegatesAsFunctionPointers && isDelegate,
                                DontExpandTypedefs = !delegatesAsFunctionPointers,
                            }
                        )
                    )
                )
                .AddAttributeLists(CreateMarshalAsAttributeLists(argument.Type.Value, false));

        if (!String.IsNullOrEmpty(argument.DeclarationDefaultValue)) {
            parameter = parameter.WithDefault(EqualsValueClause(ParseExpression(argument.DeclarationDefaultValue)));
        }

        return parameter;
    }

    private static EnumDeclarationSyntax GenerateBinding(Enum entity) {
        EnumDeclarationSyntax declarationSyntax =
            EnumDeclaration(Identifier(entity.Name))
                .AddModifiers(Token(SyntaxKind.PublicKeyword));

        if (entity.IsFlagsEnum) {
            declarationSyntax =
                declarationSyntax
                    .AddAttributeLists(
                        AttributeList(SingletonSeparatedList(Attribute(IdentifierName("Flags"))))
                    );
        }

        List<EnumMemberDeclarationSyntax> members = [];
        foreach (Enum.Member? entityMember in entity.Members) {
            EnumMemberDeclarationSyntax member =
                EnumMemberDeclaration(Identifier(entityMember.Name));

            if (!String.IsNullOrEmpty(entityMember.Value)) {
                member =
                    member
                        .WithEqualsValue(EqualsValueClause(ParseExpression(entityMember.Value)));
            }

            // HACK: automatic formatting doesn't know what to do after we add #if directives,
            // so we add the commas manually
            member = member.AddTrailingTriviaToEnd(Comment(","), LineFeed);
            member = ApplyCommonEntitySyntax(member, entityMember);

            members.Add(member);
        }

        declarationSyntax = declarationSyntax.WithMembers(
            SeparatedList(
                members,

                // HACK: see above comment
                members.Select(_ => Token(TriviaList(), SyntaxKind.CommaToken, "", "", TriviaList()))
            )
        );

        declarationSyntax = ApplyCommonEntitySyntax(declarationSyntax, entity, true);
        return declarationSyntax;
    }

    private TypeDeclarationSyntax GenerateBinding(Struct entity) {
        List<SyntaxKind> modifiers = [
            SyntaxKind.PublicKeyword,
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.PartialKeyword
        ];

        if (entity.Kind == Struct.StructKind.StaticClass) {
            modifiers.Insert(modifiers.IndexOf(SyntaxKind.PublicKeyword) + 1, SyntaxKind.StaticKeyword);
        }

        TypeDeclarationSyntax structSyntax;
        switch (entity.Kind) {
            case Struct.StructKind.StaticClass:
                structSyntax = ClassDeclaration(Identifier(entity.Name));
                break;
            case Struct.StructKind.Struct:
                structSyntax = StructDeclaration(Identifier(entity.Name));
                break;
            case Struct.StructKind.Union:
                AttributeSyntax structLayoutAttribute =
                    Attribute(ParseName("StructLayout"))
                        .WithArgumentList(AttributeArgumentList(
                            SeparatedList(
                                [
                                    AttributeArgument(ParseExpression("LayoutKind.Explicit"))
                                ]
                            )
                        ));
                structSyntax =
                    StructDeclaration(Identifier(entity.Name))
                        .AddAttributeLists(AttributeList(SeparatedList(
                            [structLayoutAttribute]
                        )));
                break;
            default:
                throw new ArgumentOutOfRangeException($"Unexpected entity kind {entity.Kind}");
        }

        structSyntax = structSyntax.AddModifiers(modifiers.Select(Token).ToArray());

        foreach (IEntity entityChild in entity.Children) {
            switch (entityChild) {
                case SyntaxEntity<MemberDeclarationSyntax> memberSyntax:
                    structSyntax = structSyntax.AddMembers(
                        ApplyCommonEntitySyntax(memberSyntax.Syntax, memberSyntax)
                    );
                    break;
                case IEntityReference { Value: Delegate delegateModel }: {
                    DelegateDeclarationSyntax delegateSyntax =
                        CreateDelegateDeclarationSyntax(delegateModel, delegateModel.Name);
                    structSyntax = structSyntax.AddMembers(delegateSyntax);
                    break;
                }
                case IEntityReference { Value: Function functionModel }: {
                    Debug.Assert(functionModel.Body != null);

                    MethodDeclarationSyntax methodSyntax =
                        GenerateFunctionBinding(
                            functionModel,
                            functionModel.OwnerStruct != null,
                            false,
                            false,
                            functionModel.Body
                        );

                    methodSyntax = ApplyCommonEntitySyntax(methodSyntax, functionModel);

                    structSyntax = structSyntax.AddMembers(methodSyntax);
                    break;
                }
                case Field field: {
                    FieldDeclarationSyntax fieldSyntax = GenerateField(entity, field, true);
                    IEntityReference? fieldTypeDelegate = field.Type.ExpandWrappers<Delegate>();
                    if (fieldTypeDelegate != null) {
                        FieldDeclarationSyntax legacyFieldSyntax = GenerateField(entity, field, false);
                        WrapInSelfExclusiveConditionals(
                            ref legacyFieldSyntax,
                            ref fieldSyntax,
                            "NETSTANDARD2_0_OR_GREATER"
                        );

                        structSyntax = structSyntax.AddMembers(fieldSyntax, legacyFieldSyntax);
                    } else {
                        structSyntax = structSyntax.AddMembers(fieldSyntax);
                    }

                    break;
                }
                default: throw new InvalidOperationException($"Unexpected struct child type {entityChild.GetType()}");
            }
        }

        structSyntax = ApplyCommonEntitySyntax(structSyntax, entity, true);

        return structSyntax;
    }

    private FieldDeclarationSyntax GenerateField(Struct entity, Field field, bool delegatesAsFunctionPointers) {
        string fieldType;
        if (delegatesAsFunctionPointers) {
            fieldType = FormatType(
                field.Type.Value,
                new FormatTypeOptions {
                    DelegatesAsFunctionPointers = true,
                    DontExpandTypedefs = true
                }
            );
        } else {
            fieldType = "IntPtr";
        }

        string fieldName = field.Name;

        Array? fieldTypeArray = field.Type.Value as Array;
        if (fieldTypeArray != null) {
            fieldType = FormatType(fieldTypeArray.ElementType.Value);
            fieldName += $"[{fieldTypeArray.ArrayBoundsNumeric?.ToString(CultureInfo.InvariantCulture) ?? fieldTypeArray.ArrayBounds}]";
        }

        VariableDeclaratorSyntax fieldVariable = VariableDeclarator(fieldName);
        if (field.Value != null) {
            fieldVariable = fieldVariable.WithInitializer(
                EqualsValueClause(ParseExpression(field.Value))
            );
        }

        FieldDeclarationSyntax fieldDeclarationSyntax =
            FieldDeclaration(
                    VariableDeclaration(
                        ParseTypeName(fieldType),
                        SeparatedList([fieldVariable])
                    )
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword));

        if (fieldTypeArray != null) {
            fieldDeclarationSyntax =
                fieldDeclarationSyntax
                    .AddModifiers(Token(SyntaxKind.FixedKeyword));
        } else if (field.IsConstant) {
            fieldDeclarationSyntax =
                fieldDeclarationSyntax
                    .AddModifiers(Token(SyntaxKind.ConstKeyword));
        }

        if (entity.Kind == Struct.StructKind.Union) {
            AttributeSyntax fieldOffsetAttribute =
                Attribute(ParseName("FieldOffset"))
                    .WithArgumentList(AttributeArgumentList(
                        SeparatedList(
                            [AttributeArgument(ParseExpression("0"))]
                        )
                    ));

            fieldDeclarationSyntax =
                fieldDeclarationSyntax
                    .AddAttributeLists(AttributeList(SeparatedList(
                        [fieldOffsetAttribute]
                    )));
        }

        fieldDeclarationSyntax = fieldDeclarationSyntax.AddAttributeLists(CreateMarshalAsAttributeLists(field.Type.Value, false));
        fieldDeclarationSyntax = ApplyCommonEntitySyntax(fieldDeclarationSyntax, field);

        return fieldDeclarationSyntax;
    }

    private static AttributeListSyntax[] CreateMarshalAsAttributeLists(Entity entity, bool isReturnValue) {
        string typeExpanded = FormatType(entity);
        if (typeExpanded == "bool") {
            return [CreateBoolMarshalAsAttribute(isReturnValue ? SyntaxKind.ReturnKeyword : null)];
        }

        return [];
    }

    private static AttributeListSyntax CreateBoolMarshalAsAttribute(SyntaxKind? target) =>
        AttributeList(
            target != null ? AttributeTargetSpecifier(Token(target.Value)) : null,
            SeparatedList([
                    Attribute(
                        ParseName("MarshalAs"),
                        AttributeArgumentList(
                            SeparatedList(
                                [AttributeArgument(ParseExpression("UnmanagedType.I1"))]
                            )
                        )
                    )
                ]
            )
        );

    private OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax> GenerateBinding(Typedef entity) {
        IEntityReference aliasedType = entity.Type;
        while (aliasedType.Value is Typedef aliasedTypeInner) {
            aliasedType = aliasedTypeInner.Type;
        }

        OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax> declarationSyntax;
        if (aliasedType.Value is Delegate userDelegate) {
            declarationSyntax = CreateDelegateDeclarationSyntax(userDelegate, entity.Name);
        } else {
            declarationSyntax =
                UsingDirective(
                        NameEquals(entity.Name),
                        IdentifierName(
                            FormatType(
                                aliasedType.Value,
                                new FormatTypeOptions {
                                    FullyQualifiedBuiltins = true,
                                    FullyQualifiedTypes = true
                                }
                            )
                        )
                    )
                    .WithGlobalKeyword(Token(SyntaxKind.GlobalKeyword))
                    .WithUnsafeKeyword(Token(SyntaxKind.UnsafeKeyword));
        }

        declarationSyntax = declarationSyntax.Match<OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax>>(
            delegateSyntax => ApplyCommonEntitySyntax(delegateSyntax, entity),
            usingSyntax => ApplyCommonEntitySyntax(usingSyntax, entity)
        );
        return declarationSyntax;
    }

    private static DelegateDeclarationSyntax CreateDelegateDeclarationSyntax(Delegate userDelegate, string name) {
        DelegateDeclarationSyntax delegateDeclaration =
            DelegateDeclaration(
                    ParseTypeName(
                        FormatType(
                            userDelegate.ReturnType.Value,
                            new FormatTypeOptions { FullyQualifiedTypes = true }
                        )
                    ),
                    userDelegate.Name
                )
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.UnsafeKeyword))
                .AddAttributeLists(CreateMarshalAsAttributeLists(userDelegate.ReturnType.Value, true))
                .AddAttributeLists(AttributeList(SeparatedList([
                        Attribute(ParseName("UnmanagedFunctionPointer"))
                            .WithArgumentList(AttributeArgumentList(
                                SeparatedList(
                                    [AttributeArgument(ParseExpression("CallingConvention.Cdecl"))]
                                )
                            ))
                    ]
                )));

        IEnumerable<ParameterSyntax> parameters =
            userDelegate.Arguments.Select(arg => CreateMethodParameterSyntax(arg, false));

        delegateDeclaration = delegateDeclaration.WithParameterList(ParameterList(SeparatedList(parameters)));
        delegateDeclaration = ApplyCommonEntitySyntax(delegateDeclaration, userDelegate);

        return delegateDeclaration;
    }

    private static CompilationUnitSyntax WrapRootLevelDefinition(string @namespace, params MemberDeclarationSyntax[] items) {
        IReadOnlyCollection<SyntaxTrivia> header = [
            Comment("// This file has been auto-generated by the 'NovaImGui.NET' generator."), LineFeed,
            Comment("// **DO NOT EDIT DIRECTLY**"), LineFeed,
            Comment("// https://github.com/LostPolygon/NovaImGui.NET"), LineFeed,
            LineFeed,
        ];

        /*UsingDirectiveSyntax CreateUsingDirective(string identifier, bool isStatic = false) {
            UsingDirectiveSyntax directive = UsingDirective(IdentifierName(identifier));
            if (isStatic) {
                directive = directive.WithStaticKeyword(Token(SyntaxKind.StaticKeyword));
            }

            return directive;
        }*/

        FileScopedNamespaceDeclarationSyntax namespaceSyntax =
            CreateNamespaceDeclarationSyntax(@namespace)
                .AddLeadingTriviaToStart(header);

        CompilationUnitSyntax compilationUnit =
            CompilationUnit()
                .AddMembers(namespaceSyntax)
                .AddMembers(items);

        return compilationUnit;
    }

    private static FileScopedNamespaceDeclarationSyntax CreateNamespaceDeclarationSyntax(string @namespace)
        => FileScopedNamespaceDeclaration(ParseName(@namespace)).WithTrailingTrivia(LineFeed, LineFeed);

    private static void WrapInSelfExclusiveConditionals<T>(ref T syntax, ref T oppositeSyntax, string expression) where T : CSharpSyntaxNode {
        syntax = ApplyConditionals(
            syntax,
            [new Conditional("if", expression)]
        );

        oppositeSyntax = ApplyConditionals(
            oppositeSyntax,
            [new Conditional("if", "!" + expression)]
        );
    }

    private static T ApplyCommonEntitySyntax<T>(
        T syntaxNode,
        IIndependentEntity entity,
        bool isTopLevelEntity = false
    ) where T : CSharpSyntaxNode {
        syntaxNode = ApplyComments(syntaxNode, entity.Comments, isTopLevelEntity);
        syntaxNode = ApplyConditionals(syntaxNode, entity.Conditionals);

        if (entity.IsDisabled) {
            syntaxNode = syntaxNode.AddLeadingTriviaToStart(Comment("/*"));
            syntaxNode = syntaxNode.AddTrailingTriviaToEnd(Comment("*/"));
        }

        syntaxNode = syntaxNode.AddDocumentationComment(entity.Comments.Notes, false, true);

        return syntaxNode;
    }

    private static T ApplyConditionals<T>(T syntaxNode, IReadOnlyCollection<Conditional> conditionals) where T : CSharpSyntaxNode {
        if (conditionals.Count == 0)
            return syntaxNode;

        foreach ((string condition, string expression) in conditionals) {
            string expressionFormatted = condition switch {
                "if" => expression,
                "ifdef" => expression,
                "ifndef" => $"!({expression})",
                "ifnot" => $"!({expression})",
                _ => throw new ArgumentOutOfRangeException(nameof(condition), condition, "Unexpected condition")
            };

            StructuredTriviaSyntax start = IfDirectiveTrivia(
                ParseExpression(expressionFormatted),
                false,
                true,
                true
            ).AddTrailingTriviaToEnd(LineFeed);

            EndIfDirectiveTriviaSyntax end = EndIfDirectiveTrivia(true);

            syntaxNode = syntaxNode.AddLeadingTriviaToStart(Trivia(start));
            syntaxNode = syntaxNode.AddTrailingTriviaToEnd(Trivia(end));
        }

        return syntaxNode;
    }

    private static T ApplyComments<T>(T item, Comments comments, bool isTopLevelEntity)
        where T : CSharpSyntaxNode {
        List<string> attachedReformatted = [];
        foreach (string attachedComment in comments.Attached) {
            IEnumerable<string> attachedCommentChunks =
                attachedComment
                    .Split("// ", StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim());
            attachedReformatted.AddRange(attachedCommentChunks);
        }

        if (isTopLevelEntity) {
            string[] mergedXmlComments = [..comments.Preceding, ..attachedReformatted];
            item = item.AddDocumentationComment(mergedXmlComments, true);
        } else {
            item = item.AddDocumentationComment(comments.Preceding, false);
            item = item.AddDocumentationComment(attachedReformatted, true);
        }

        return item;
    }
}

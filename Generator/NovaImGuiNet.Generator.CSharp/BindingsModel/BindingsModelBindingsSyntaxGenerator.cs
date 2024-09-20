//#define DISABLE_PARALLEL

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq.Extensions;
using OneOf;

namespace NovaImGuiNet.Generator.CSharp;

using static CSharpCodeGenerationUtility;
using static BindingsModel;
using static SyntaxFactory;
using static SyntaxFactoryCached;

public class BindingsModelBindingsSyntaxGenerator {
    public BindingsModel BindingsModel { get; }
    public BindingsContext BindingsContext { get; }

    public BindingsModelBindingsSyntaxGenerator(
        BindingsModel metadataModel,
        BindingsContext bindingsContext
    ) {
        BindingsModel = metadataModel;
        BindingsContext = bindingsContext;
    }

    public IReadOnlyList<BindingsCompilationUnit> Generate() {
        ConcurrentBag<BindingsCompilationUnit> compilationUnits = [];
        List<Action> generationActions = [];

        void AddCompilationUnit(
            OneOf<ITopLevelEntity, CompilationUnitFileInformation> entityOrFileInformation,
            CSharpSyntaxNode syntaxNode,
            string? category = null
        ) {
            compilationUnits.Add(
                new BindingsCompilationUnit(
                    entityOrFileInformation,
                    syntaxNode,
                    category
                )
            );
        }

        void AddTask(Action action) {
            generationActions.Add(action);
        }

        void AddItemWrapperTask<T>(IEnumerable<EntityReferenceWrapper<T>> enumerable, Action<T> action) where T : TopLevelEntity {
            foreach (EntityReferenceWrapper<T> item in enumerable) {
                if (item.Value.NoEmit)
                    continue;

                generationActions.Add(() => action(item.Value));
            }
        }

        AddTask(() => {
            IEnumerable<CompilationUnitWithFileInformation> functionsCompilationUnits = GenerateNativeFunctionBindingsClass();
            foreach (CompilationUnitWithFileInformation functionsCompilationUnit in functionsCompilationUnits) {
                AddCompilationUnit(
                    functionsCompilationUnit.File,
                    functionsCompilationUnit.CompilationUnitSyntax
                );
            }
        });

        AddItemWrapperTask(
            BindingsModel.Structs,
            structure => {
                CompilationUnitSyntax compilationUnitSyntax = WrapRootLevelDefinition(
                    structure.Namespace!,
                    GenerateBinding(structure)
                );

                AddCompilationUnit(structure, compilationUnitSyntax, "Structs");
            }
        );

        AddTask(() => {
            (
                IReadOnlyList<(CompilationUnitSyntax compilationUnit, string ns)> globalUsingsCompilationUnits,
                IReadOnlyList<(CompilationUnitSyntax compilationUnit, string ns)> delegatesCompilationUnits
            ) = GenerateTypedefBindings();

            foreach ((CompilationUnitSyntax compilationUnit, string ns) globalUsingsCompilationUnit in globalUsingsCompilationUnits) {
                AddCompilationUnit(
                    new CompilationUnitFileInformation(
                        "GlobalUsings",
                        globalUsingsCompilationUnit.ns
                    ),
                    globalUsingsCompilationUnit.compilationUnit
                );
            }

            foreach ((CompilationUnitSyntax compilationUnit, string ns) delegatesCompilationUnit in delegatesCompilationUnits) {
                AddCompilationUnit(
                    new CompilationUnitFileInformation(
                        "Delegates",
                        delegatesCompilationUnit.ns
                    ),
                    delegatesCompilationUnit.compilationUnit
                );
            }
        });

        AddItemWrapperTask(
            BindingsModel.Enums,
            enumeration => {
                CompilationUnitSyntax compilationUnitSyntax = WrapRootLevelDefinition(
                    enumeration.Namespace!,
                    GenerateBinding(enumeration)
                );

                AddCompilationUnit(enumeration, compilationUnitSyntax, "Enums");
            }
        );

#if DISABLE_PARALLEL
        generationActions.ForEach(action => action());
#else
        Parallel.ForEach(
            Partitioner.Create(generationActions.ToArray(), true),
            action => action()
        );
#endif

        BindingsModel.ExtraCompilationUnits
            .Select(unit => new BindingsCompilationUnit(unit.File, unit.CompilationUnitSyntax, unit.File.Category))
            .ForEach(unit => compilationUnits.Add(unit));

        return compilationUnits.ToArray();
    }

    private IEnumerable<CompilationUnitWithFileInformation> GenerateNativeFunctionBindingsClass() {
        IGrouping<string?, EntityReferenceWrapper<Function>>[] allNativeFunctionsByNamespace =
            BindingsModel
                .GetAllFunctions()
                .Where(x => x.Value.IsNativeFunction)
                .GroupBy(x => x.Value.Namespace)
                .ToArray();

        foreach (IGrouping<string?, EntityReferenceWrapper<Function>> entityReferenceWrappers in allNativeFunctionsByNamespace) {
            string? targetNamespace = entityReferenceWrappers.Key;
            Debug.Assert(targetNamespace != null);

            EntityReferenceWrapper<Function>[] allNativeFunctions = entityReferenceWrappers.ToArray();

            List<MemberDeclarationSyntax> methodSyntaxes = [];
            foreach (EntityReferenceWrapper<Function> functionReference in allNativeFunctions) {
                Function apiModelFunction = functionReference.Value;
                methodSyntaxes.AddRange(GenerateNativeFunctionDeclaration(apiModelFunction));
            }

            string targetClassName =
                BindingsContext.Customizations
                    .RequireCustomizationByNamespace(targetNamespace)
                    .NativeFunctionsClassName;

            ClassDeclarationSyntax classDeclaration =
                ClassDeclaration(Identifier(targetClassName))
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
                    .AddMembers(methodSyntaxes.ToArray());

            CompilationUnitSyntax compilationUnitSyntax = WrapRootLevelDefinition(
                targetNamespace,
                classDeclaration
            );

            yield return
                new CompilationUnitWithFileInformation(
                    compilationUnitSyntax,
                    new CompilationUnitFileInformation(targetClassName, targetNamespace, ForceTopLevel: true)
                );
        }
    }

    private(
        IReadOnlyList<(CompilationUnitSyntax compilationUnit, string ns)> globalUsingsCompilationUnit,
        IReadOnlyList<(CompilationUnitSyntax compilationUnit, string ns)> delegatesCompilationUnits
        ) GenerateTypedefBindings() {
        (string @namespace, List<OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax>> typedefBindingsNamespace)[] typedefBindings =
            BindingsModel.Typedefs
                .Select(t => t.Value)
                .OrderBy(t => t.Type.Value is Delegate)
                .Where(t => !t.NoEmit)
                .GroupBy(t => t.Namespace)
                .Select(typedefsByNamespace => {
                    List<OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax>> typedefBindingsNamespace = [];
                    foreach (Typedef typedef in typedefsByNamespace) {
                        typedefBindingsNamespace.Add(GenerateBinding(typedef));
                    }

                    return (typedefsByNamespace.Key!, typedefBindingsNamespace);
                })
                .ToArray();

        List<(CompilationUnitSyntax compilationUnit, string ns)> delegatesCompilationUnits = [];
        List<(CompilationUnitSyntax compilationUnit, string ns)> globalUsingsCompilationUnits = [];

        foreach ((string ns, List<OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax>> typedefs) in typedefBindings) {
            List<DelegateDeclarationSyntax> delegates = [];
            List<UsingDirectiveSyntax> globalUsings = [];

            foreach (OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax> typedef in typedefs) {
                typedef.Switch(
                    delegateBinding => delegates.Add(delegateBinding),
                    globalUsingBinding => globalUsings.Add(globalUsingBinding)
                );
            }

            if (globalUsings.Count != 0) {
                globalUsingsCompilationUnits.Add(
                    (
                        CompilationUnit().AddUsings(globalUsings.ToArray()),
                        ns
                    )
                );
            }

            if (delegates.Count != 0) {
                delegatesCompilationUnits.Add(
                    (
                        CompilationUnit()
                            .AddMembers(CreateNamespaceDeclarationSyntax(ns))
                            .AddMembers(delegates.Cast<MemberDeclarationSyntax>().ToArray()),
                        ns
                    )
                );
            }
        }

        return (globalUsingsCompilationUnits, delegatesCompilationUnits);
    }

    private IReadOnlyList<BaseMethodDeclarationSyntax> GenerateNativeFunctionDeclaration(Function function) {
        IEntityReference[] allChildTypes = function.GetReferencedTypes([]).ToArray();
        IEntityReference[] callbacks = [
            ..allChildTypes.Where(x => x.Value is Delegate),
            ..allChildTypes.Where(t => t.Value is Typedef { Type.Value: Delegate })
        ];

        if (callbacks.Length != 0) {
            BaseMethodDeclarationSyntax managedMethodSyntax = Generate(function, false);
            BaseMethodDeclarationSyntax unmanagedMethodSyntax = Generate(function, true);
            unmanagedMethodSyntax = ApplyConditionals(
                unmanagedMethodSyntax,
                [new Conditional("NET6_0_OR_GREATER")]
            );

            return [managedMethodSyntax, unmanagedMethodSyntax];
        }

        return [Generate(function, false)];

        // When delegatesAsFunctionPointers is true, generates delegates unmanaged function pointers
        // When delegatesAsFunctionPointers is false, generates delegates as IntPtr
        BaseMethodDeclarationSyntax Generate(Function function, bool delegatesAsFunctionPointers) {
#if USE_LIBRARY_IMPORT
                List<AttributeSyntax> importAttributes = [
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
#else
            List<AttributeSyntax> importAttributes = [
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
#endif

            // Mangle the function by replacing all delegates with IntPtr
            if (!delegatesAsFunctionPointers) {
                List<FunctionArgument> newArguments =
                    function.Arguments
                        .Select(arg => {
                            // This two-level nonsense deals with ImGui.GetAllocatorFunctions
                            // pointers to function pointers
                            Delegate? firstLevelDelegate = arg.Type.ExpandTypedef().Value as Delegate;
                            Delegate? secondLevelDelegate = arg.Type.ExpandAllTypedefsAndPointers().Value as Delegate;
                            if (firstLevelDelegate == null && secondLevelDelegate == null)
                                return arg;

                            if (firstLevelDelegate == null && secondLevelDelegate != null) {
                                arg = arg with { Type = KnownEntities.VoidVoidPointer.AsEntityReference() };
                            } else {
                                arg = arg with { Type = KnownEntities.IntPtr.AsEntityReference() };
                            }

                            return arg;
                        })
                        .ToList();

                function = function with {
                    Arguments = newArguments
                };
            }

            BaseMethodDeclarationSyntax methodSyntax =
                GenerateFunctionBindingMethodSyntax(
                        function,
                        true,
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
                    )
#if USE_LIBRARY_IMPORT
                    .AddModifiers(Token(SyntaxKind.PartialKeyword));
#else
                    .AddModifiers(Token(SyntaxKind.ExternKeyword));
#endif

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

            methodSyntax = methodSyntax.AddLeadingTriviaToEnd(generatorTrivia);

            methodSyntax = ApplyCommonEntitySyntax(methodSyntax, function);
            return methodSyntax;
        }
    }

    private BaseMethodDeclarationSyntax GenerateFunctionBindingMethodSyntax(
        Function function,
        bool isStaticMethod,
        bool isNativeMethod,
        bool delegatesAsFunctionPointers,
        OneOf<BlockSyntax, ArrowExpressionClauseSyntax>? bodySyntax
    ) {
        List<SyntaxKind> modifiers = [SyntaxKind.PublicKeyword];

        if (isStaticMethod) {
            modifiers.Add(SyntaxKind.StaticKeyword);
        }

        BaseMethodDeclarationSyntax methodDeclarationSyntax;
        if (function.IsConstructor && !isNativeMethod) {
            methodDeclarationSyntax =
                ConstructorDeclaration(
                        FormatType(function.ReturnType.Value)
                    )
                    .AddModifiers(modifiers.Select(Token).ToArray());
        } else {
            // Uglyness for ref return support
            TypeSyntax returnTypeSyntax;
            if (function.ReturnType.Value is Pointer { PointerType: BindingsModel.PointerType.ManagedRef } pointer) {
                returnTypeSyntax = RefType(ParseTypeName(FormatType(pointer.ValueType.Value)));
            } else {
                returnTypeSyntax = ParseTypeName(FormatType(function.ReturnType.Value));
            }

            methodDeclarationSyntax =
                MethodDeclaration(
                        returnTypeSyntax,
                        function.Name
                    )
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
                    blockSyntax => methodDeclarationSyntaxCopy.WithBody(blockSyntax),
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

    private ParameterSyntax CreateMethodParameterSyntax(
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
                parameter = parameter.AddAttributeLists(AttributeList(SeparatedList([
                    Attribute(
                        ParseName("ImGuiDefaultValue"),
                        AttributeArgumentList(
                            SeparatedList(
                                [
                                    AttributeArgument(
                                        LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(argument.DefaultValue))
                                    )
                                ]
                            )
                        )
                    )
                ])));
            }
        }

        if (argument.Attributes.Count != 0) {
            parameter = parameter.AddAttributeLists(AttributeList(SeparatedList(argument.Attributes)));
        }

        return parameter;
    }

    private EnumDeclarationSyntax GenerateBinding(Enum entity) {
        EnumDeclarationSyntax enumDeclaration =
            EnumDeclaration(Identifier(entity.Name))
                .AddModifiers(Token(SyntaxKind.PublicKeyword));

        if (entity.UnderlyingType is { Value: not Builtin { BuiltinName: "int" } }) {
            enumDeclaration =
                enumDeclaration
                    .AddBaseListTypes(SimpleBaseType(ParseTypeName(
                        FormatType(entity.UnderlyingType.Value)
                    )));
        }

        if (entity.IsFlags) {
            enumDeclaration =
                enumDeclaration
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

        enumDeclaration = enumDeclaration.WithMembers(
            SeparatedList(
                members,

                // HACK: see above comment
                members.Select(_ => Token(TriviaList(), SyntaxKind.CommaToken, "", "", TriviaList()))
            )
        );

        enumDeclaration = ApplyCommonEntitySyntax(enumDeclaration, entity, true);
        return enumDeclaration;
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
        List<MemberDeclarationSyntax> structMembers = [];
        foreach (IEntity entityChild in entity.Children) {
            switch (entityChild) {
                case SyntaxEntity<MemberDeclarationSyntax> memberSyntax:
                    memberSyntax.Syntax = ApplyCommonEntitySyntax(memberSyntax.Syntax, memberSyntax);
                    structMembers.Add(memberSyntax.Syntax);
                    break;
                case Delegate delegateModel: {
                    DelegateDeclarationSyntax delegateSyntax =
                        CreateDelegateDeclarationSyntax(delegateModel, delegateModel.Name);
                    structMembers.Add(delegateSyntax);
                    break;
                }
                case Function functionModel: {
                    Debug.Assert(functionModel.Body != null);

                    BaseMethodDeclarationSyntax methodSyntax =
                        GenerateFunctionBindingMethodSyntax(
                            functionModel,
                            functionModel.OwnerStruct == null || functionModel.IsStatic,
                            false,
                            false,
                            functionModel.Body
                        );

                    methodSyntax = ApplyCommonEntitySyntax(methodSyntax, functionModel);
                    structMembers.Add(methodSyntax);
                    break;
                }
                case Field field: {
                    FieldDeclarationSyntax fieldSyntax = GenerateField(entity, field, true);
                    IEntityReference? fieldTypeDelegate = field.Type.ExpandAllTypedefsAndPointers<Delegate>();
                    if (fieldTypeDelegate != null) {
                        FieldDeclarationSyntax legacyFieldSyntax = GenerateField(entity, field, false);
                        WrapInSelfExclusiveConditionals(
                            ref legacyFieldSyntax,
                            ref fieldSyntax,
                            "NETSTANDARD2_0_OR_GREATER"
                        );

                        structMembers.Add(fieldSyntax);
                        structMembers.Add(legacyFieldSyntax);
                    } else {
                        structMembers.Add(fieldSyntax);
                    }

                    break;
                }
                default: throw new InvalidOperationException($"Unexpected struct child type {entityChild.GetType()}");
            }
        }

        if (entity.Kind != Struct.StructKind.StaticClass) {
            structSyntax = structSyntax
                .WithBaseList(
                    BaseList(SeparatedList<BaseTypeSyntax>().Add(
                        SimpleBaseType(ParseTypeName($"IEquatable<{entity.Name}>"))
                    ))
                );

            // FIXME: this is terrible for perf, we need to actually generate
            // Equals(T other) and GetHashCode() manually someday
            structMembers.AddRange([
                ParseMemberDeclaration($"public bool Equals({entity.Name} other) => base.Equals(other);\n"),
                ParseMemberDeclaration($"public bool Equals(in {entity.Name} other) => Unsafe.AreSame(in this, in other) || Equals(other);\n"),

                ParseMemberDeclaration($"public override int GetHashCode() => base.GetHashCode();\n"),
                ParseMemberDeclaration($"public override bool Equals(object obj) => base.Equals(obj);\n"),

                ParseMemberDeclaration($"public static bool operator ==(in {entity.Name} left, in {entity.Name} right) => left.Equals(in right);\n"),
                ParseMemberDeclaration($"public static bool operator !=(in {entity.Name} left, in {entity.Name} right) => !left.Equals(in right);\n"),
            ]);
        }

        structSyntax = structSyntax.AddMembers(structMembers.ToArray());
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
                    ExpandTypedefs = false
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

        // FIXME: disabled for non-static classes as it would need a separate public constructor for a struct,
        // which is not supported in all C# versions
        if (field.IsConstant && field.Value != null) {
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
        if (entity.AsEntityReference().ExpandTypedef().Value is Builtin builtin &&
            builtin.ManagedType == typeof(bool)) {
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
                                    FullyQualifiedTypes = true,
                                    ExpandTypedefs = true // Global usings can't reference other global usings
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

    private DelegateDeclarationSyntax CreateDelegateDeclarationSyntax(Delegate userDelegate, string name) {
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
            userDelegate.Arguments.Select(arg => CreateMethodParameterSyntax(arg, false, true));

        delegateDeclaration = delegateDeclaration.WithParameterList(ParameterList(SeparatedList(parameters)));
        delegateDeclaration = ApplyCommonEntitySyntax(delegateDeclaration, userDelegate);

        return delegateDeclaration;
    }

    private static CompilationUnitSyntax WrapRootLevelDefinition(
        string targetNamespace,
        params MemberDeclarationSyntax[] items
    ) {
        IReadOnlyCollection<string> headerLines = [
            $"// <auto-generated>",
            $"//   This code was generated by the 'NovaImGui.NET' generator.",
            $"//   Generator Version: {ThisAssembly.AssemblyInformationalVersion}",
            $"//",
            $"//   Changes to this file may cause incorrect behavior ",
            $"//   and will be lost if the code is regenerated.",
            $"//",
            $"//   https://github.com/LostPolygon/NovaImGui.NET",
            $"// </auto-generated>",
            $""
        ];

        SyntaxTrivia[] headerTrivia =
            headerLines
                .Select(Comment)
                .Interleave(headerLines.Select(_ => LineFeed))
                .ToArray();

        /*UsingDirectiveSyntax CreateUsingDirective(string identifier, bool isStatic = false) {
            UsingDirectiveSyntax directive = UsingDirective(IdentifierName(identifier));
            if (isStatic) {
                directive = directive.WithStaticKeyword(Token(SyntaxKind.StaticKeyword));
            }

            return directive;
        }*/

        FileScopedNamespaceDeclarationSyntax namespaceSyntax =
            CreateNamespaceDeclarationSyntax(targetNamespace)
                .AddLeadingTriviaToStart(headerTrivia);

        CompilationUnitSyntax compilationUnit =
            CompilationUnit()
                .AddMembers(namespaceSyntax)
                .AddMembers(items);

        return compilationUnit;
    }

    private static FileScopedNamespaceDeclarationSyntax CreateNamespaceDeclarationSyntax(string @namespace)
        => FileScopedNamespaceDeclaration(ParseName(@namespace)).WithTrailingTrivia(LineFeed, LineFeed);

    private static void WrapInSelfExclusiveConditionals<T>(
        ref T syntax,
        ref T oppositeSyntax,
        string expression
    ) where T : CSharpSyntaxNode {
        syntax = ApplyConditionals(
            syntax,
            [new Conditional("if", expression)]
        );

        oppositeSyntax = ApplyConditionals(
            oppositeSyntax,
            [new Conditional("if", "!" + expression)]
        );
    }

    private T ApplyCommonEntitySyntax<T>(
        T syntaxNode,
        IIndependentEntity entity,
        bool isTopLevelEntity = false
    ) where T : CSharpSyntaxNode {
        if (BindingsContext.Options.GenerateXmlDocumentationComments) {
            syntaxNode = ApplyComments(syntaxNode, entity.Comments, true || isTopLevelEntity);
        }

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

//#define DISABLE_PARALLEL

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MoreLinq.Extensions;
using OneOf;

namespace NovaImGuiNet.Generator.CSharp;

using static CSharpCodeGenerationUtility;
using static BindingsModel;
using static SyntaxFactory;
using static SyntaxFactoryCached;
using static BindingsGenerationUtility;
using static BindingsMethodSyntaxGenerator;

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

        ImGuiNativeBindingsGenerator nativeBindingsGenerator = new(BindingsModel, BindingsContext);
        foreach (bool isUntyped in new[] { false, true }) {
            AddTask(() => {
                IEnumerable<CompilationUnitWithFileInformation> functionsCompilationUnits =
                    nativeBindingsGenerator.GenerateNativeFunctionBindingsClass(isUntyped);

                foreach (CompilationUnitWithFileInformation functionsCompilationUnit in functionsCompilationUnits) {
                    AddCompilationUnit(
                        functionsCompilationUnit.File,
                        functionsCompilationUnit.CompilationUnitSyntax
                    );
                }
            });
        }

        AddItemWrapperTask(
            BindingsModel.Structs,
            structure => {
                CompilationUnitSyntax compilationUnitSyntax = WrapRootLevelDefinition(
                    structure.Namespace!,
                    GenerateStructBinding(structure)
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
                    GenerateEnumBinding(enumeration)
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

    private EnumDeclarationSyntax GenerateEnumBinding(Enum entity) {
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

    private TypeDeclarationSyntax GenerateStructBinding(Struct entity) {
        List<SyntaxKind> modifiers = [
            SyntaxKind.PublicKeyword,
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.PartialKeyword
        ];

        TypeDeclarationSyntax structSyntax;
        switch (entity.Kind) {
            case Struct.StructKind.StaticClass:
                structSyntax = ClassDeclaration(Identifier(entity.Name));
                modifiers.Insert(modifiers.IndexOf(SyntaxKind.PublicKeyword) + 1, SyntaxKind.StaticKeyword);
                break;
            case Struct.StructKind.Struct:
                structSyntax = StructDeclaration(Identifier(entity.Name));
                break;
            case Struct.StructKind.Union:
                structSyntax =
                    StructDeclaration(Identifier(entity.Name))
                        .AddAttributeLists(
                            SimpleAttributeList("StructLayout", "LayoutKind.Explicit")
                        );
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
                        CreateDelegateDeclarationSyntax(delegateModel);
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
                    FieldDeclarationSyntax fieldSyntax = GenerateField(entity, field);
                    structMembers.Add(fieldSyntax);

                    break;
                }
                default: throw new InvalidOperationException($"Unexpected struct child type {entityChild.GetType()}");
            }
        }

        if (entity.Kind is Struct.StructKind.Struct or Struct.StructKind.Union) {
            structSyntax = structSyntax
                .WithBaseList(
                    BaseList(SeparatedList<BaseTypeSyntax>().Add(
                        SimpleBaseType(ParseTypeName($"IEquatable<{entity.Name}>"))
                    ))
                );

            // FIXME: this is terrible for perf, we need to actually generate
            // Equals(T other) and GetHashCode() manually someday
            structMembers.AddRange([
                ParseMemberDeclaration($"public readonly bool Equals({entity.Name} other) => base.Equals(other);\n"),
                ParseMemberDeclaration($"public readonly bool Equals(in {entity.Name} other) => UnsafeUtility.AreRefSame(in this, in other) || Equals(other);\n"),

                ParseMemberDeclaration($"public readonly override int GetHashCode() => base.GetHashCode();\n"),
                ParseMemberDeclaration($"public readonly override bool Equals(object obj) => base.Equals(obj);\n"),

                ParseMemberDeclaration($"public static bool operator ==(in {entity.Name} left, in {entity.Name} right) => left.Equals(in right);\n"),
                ParseMemberDeclaration($"public static bool operator !=(in {entity.Name} left, in {entity.Name} right) => !left.Equals(in right);\n"),
            ]);
        }

        structSyntax = structSyntax.AddMembers(structMembers.ToArray());
        structSyntax = ApplyCommonEntitySyntax(structSyntax, entity, true);

        return structSyntax;
    }

    private FieldDeclarationSyntax GenerateField(Struct entity, Field field) {
        string fieldType = FormatType(
            field.Type.Value,
            new FormatTypeOptions {
                DelegatesAsFunctionPointers = true,
                ExpandTypedefs = false
            }
        );

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
            fieldDeclarationSyntax =
                fieldDeclarationSyntax
                    .AddAttributeLists(SimpleAttributeList("FieldOffset", "0"));
        }

        fieldDeclarationSyntax = fieldDeclarationSyntax.AddAttributeLists(CreateMarshalAsAttributeLists(field.Type.Value, false));
        fieldDeclarationSyntax = ApplyCommonEntitySyntax(fieldDeclarationSyntax, field);

        return fieldDeclarationSyntax;
    }

    private OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax> GenerateBinding(Typedef entity) {
        IEntityReference aliasedType = entity.Type;
        while (aliasedType.Value is Typedef aliasedTypeInner) {
            aliasedType = aliasedTypeInner.Type;
        }

        OneOf<DelegateDeclarationSyntax, UsingDirectiveSyntax> declarationSyntax;
        if (aliasedType.Value is Delegate userDelegate) {
            declarationSyntax = CreateDelegateDeclarationSyntax(userDelegate);
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

    private DelegateDeclarationSyntax CreateDelegateDeclarationSyntax(Delegate userDelegate) {
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
                .AddAttributeLists(SimpleAttributeList("UnmanagedFunctionPointer", "CallingConvention.Cdecl"));

        IEnumerable<ParameterSyntax> parameters =
            userDelegate.Arguments.Select(arg => CreateMethodParameterSyntax(arg, false, true));

        delegateDeclaration = delegateDeclaration.WithParameterList(ParameterList(SeparatedList(parameters)));
        delegateDeclaration = ApplyCommonEntitySyntax(delegateDeclaration, userDelegate);

        return delegateDeclaration;
    }

    private T ApplyCommonEntitySyntax<T>(
        T syntaxNode,
        IIndependentEntity entity,
        bool isTopLevelEntity = false
    ) where T : CSharpSyntaxNode {
        return BindingsGenerationUtility.ApplyCommonEntitySyntax(syntaxNode, entity, BindingsContext.Options.GenerateXmlDocumentationComments, isTopLevelEntity);
    }
}

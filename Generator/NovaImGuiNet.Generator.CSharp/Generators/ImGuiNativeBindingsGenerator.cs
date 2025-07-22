//#define USE_LIBRARY_IMPORT

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NovaImGuiNet.Generator.CSharp;

using static CSharpCodeGenerationUtility;
using static BindingsModel;
using static SyntaxFactory;
using static SyntaxFactoryCached;
using static BindingsGenerationUtility;
using static BindingsMethodSyntaxGenerator;

public class ImGuiNativeBindingsGenerator {
    public BindingsModel BindingsModel { get; }
    public BindingsContext BindingsContext { get; }

    public ImGuiNativeBindingsGenerator(
        BindingsModel metadataModel,
        BindingsContext bindingsContext
    ) {
        BindingsModel = metadataModel;
        BindingsContext = bindingsContext;
    }

    public IEnumerable<CompilationUnitWithFileInformation> GenerateNativeFunctionBindingsClass(bool useRawPointers) {
        bool isInnerClass = useRawPointers;

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
                methodSyntaxes.AddRange(GenerateNativeFunctionDeclaration(apiModelFunction, useRawPointers));
            }

            BindingsFileCustomizations customizations =
                BindingsContext.Customizations
                    .RequireCustomizationByNamespace(targetNamespace);

            string targetClassName = customizations.NativeFunctionsClassName;
            string nativeLibraryName = customizations.NativeLibraryName;
            string fileName = targetClassName;

            if (useRawPointers) {
                targetClassName = customizations.GetNativeFunctionsRawPointersInnerClassName();
                fileName = customizations.GetNativeFunctionsRawPointersInnerClassFullName();
            }

            ClassDeclarationSyntax classDeclaration = CreateClassDeclarationSyntax(targetClassName);
            if (!isInnerClass) {
                classDeclaration = classDeclaration
                    .AddMembers(
                        ParseMemberDeclaration($"public const string ImGuiDllName = \"{nativeLibraryName}\";\n"),
                        ApplyConditionals(
                            ParseMemberDeclaration("public const string ImGuiDllName = \"__Internal\";\n"),
                            new Conditional("NOVA_STATIC_LINK")
                        ),
                        ParseMemberDeclaration("public const CallingConvention ImGuiCallingConvention = CallingConvention.Cdecl;")!
                    );
            }

            classDeclaration = classDeclaration.AddMembers(methodSyntaxes.ToArray());

            if (isInnerClass) {
                ClassDeclarationSyntax outerClassDeclaration = CreateClassDeclarationSyntax(customizations.NativeFunctionsClassName);
                outerClassDeclaration = outerClassDeclaration.AddMembers(classDeclaration);
                classDeclaration = outerClassDeclaration;
            }

            CompilationUnitSyntax compilationUnitSyntax = WrapRootLevelDefinition(
                targetNamespace,
                classDeclaration
            );

            yield return
                new CompilationUnitWithFileInformation(
                    compilationUnitSyntax,
                    new CompilationUnitFileInformation(fileName, targetNamespace, ForceTopLevel: true)
                );
        }

        static ClassDeclarationSyntax CreateClassDeclarationSyntax(string targetClassName) =>
            ClassDeclaration(Identifier(targetClassName))
                .AddModifiers(
                    Token(SyntaxKind.PublicKeyword),
                    Token(SyntaxKind.StaticKeyword),
                    Token(SyntaxKind.UnsafeKeyword),
                    Token(SyntaxKind.PartialKeyword)
                );
    }

    private IReadOnlyList<BaseMethodDeclarationSyntax> GenerateNativeFunctionDeclaration(Function function, bool useRawPointers) {
        BaseMethodDeclarationSyntax managedMethodSyntaxRaw = Generate(false);
        BaseMethodDeclarationSyntax managedMethodSyntax =
            ApplyCommonEntitySyntax(
                managedMethodSyntaxRaw,
                function,
                BindingsContext.Options.GenerateXmlDocumentationComments
            );

        IEntityReference[] allReferencedTypes = function.GetReferencedTypes([]).ToArray();
        IEntityReference[] callbacks = [
            ..allReferencedTypes.Where(x => x.Value is Delegate),
            ..allReferencedTypes.Where(t => t.Value is Typedef { Type.Value: Delegate })
        ];

        if (callbacks.Length != 0) {
            BaseMethodDeclarationSyntax unmanagedMethodSyntaxRaw = Generate(true);

            if (managedMethodSyntaxRaw.IsEquivalentTo(unmanagedMethodSyntaxRaw))
                return [managedMethodSyntax];

            BaseMethodDeclarationSyntax unmanagedMethodSyntax =
                ApplyCommonEntitySyntax(
                    unmanagedMethodSyntaxRaw,
                    function,
                    BindingsContext.Options.GenerateXmlDocumentationComments
                );
            unmanagedMethodSyntax = ApplyConditionals(
                unmanagedMethodSyntax,
                new Conditional("NET6_0_OR_GREATER")
            );

            return [managedMethodSyntax, unmanagedMethodSyntax];
        }

        return [managedMethodSyntax];

        // When delegatesAsFunctionPointers is true, generates delegates unmanaged function pointers
        // When delegatesAsFunctionPointers is false, generates delegates as IntPtr
        BaseMethodDeclarationSyntax Generate(bool delegatesAsFunctionPointers) {
#if USE_LIBRARY_IMPORT
            List<AttributeSyntax> importAttributes = [
                SimpleAttribute("LibraryImport", "ImGuiDllName", "StringMarshalling = StringMarshalling.Utf8"),
                SimpleAttribute("UnmanagedCallConv", "CallConvs = [typeof(CallConvCdecl)]")
            ];
#else
            List<AttributeSyntax> importAttributes = [
                SimpleAttribute("DllImport", "ImGuiDllName", "CallingConvention = ImGuiCallingConvention")
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

            if (useRawPointers) {
                List<FunctionArgument> newArguments =
                    function.Arguments
                        .Select(arg => {
                            if (arg.Type.ExpandTypedef().Value is not Pointer)
                                return arg;

                            arg = arg with { Type = KnownEntities.IntPtr.AsEntityReference() };
                            return arg;
                        })
                        .ToList();

                function = function with {
                    Arguments = newArguments
                };

                if (function.ReturnType.ExpandTypedef().Value is Pointer) {
                    function = function with {
                        ReturnType = KnownEntities.IntPtr.AsEntityReference()
                    };
                }
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

            if (function.IsConstructor) {
                generatorCommentItems.Add("IsConstructor");
            }

            string generatorComment = String.Join(", ", generatorCommentItems);
            SyntaxTrivia[] generatorTrivia = [
                Comment($"// {generatorComment}"),
                LineFeed,
                LineFeed
            ];

            methodSyntax = methodSyntax.AddLeadingTriviaToEnd(generatorTrivia);

            return methodSyntax;
        }
    }
}

using CaseConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static BindingsModel;
using static CSharpCodeGenerationUtility;
using static SyntaxFactory;
using static SyntaxFactoryCached;

public class ManagedMethodGenerator(BindingsContext bindingsContext, BindingsModel model) {
    private static readonly ILogger Log = Logging.Create(nameof(ManagedMethodGenerator));

    public BindingsModel Generate() {
        IGrouping<EntityReferenceWrapper<Struct>?, EntityReferenceWrapper<Function>>[] allFunctionsByStruct =
            model.GetAllFunctions()
                .GroupBy(x => x.Value.OwnerStruct)
                .ToArray();

        foreach (var functionsByStruct in allFunctionsByStruct) {
            EntityReferenceWrapper<Struct>? ownerStruct = functionsByStruct.Key;
            EntityReferenceWrapper<Function>[] structFunctions = functionsByStruct.ToArray();
            if (ownerStruct == null) {
                IGrouping<string?, EntityReferenceWrapper<Function>>[] allFreeFunctionsByNamespaces =
                    structFunctions
                        .GroupBy(x => x.Value.Namespace)
                        .ToArray();

                foreach (var freeFunctionsByNamespace in allFreeFunctionsByNamespaces) {
                    Struct imGuiMethodsStruct = CreateImGuiManagedMethodsStruct(
                        freeFunctionsByNamespace.Key,
                        bindingsContext.Customizations
                            .RequireCustomizationByNamespace(freeFunctionsByNamespace.Key)
                            .NativeFunctionsManagedWrapperClassName
                    );
                    IEntityReference imGuiMethodsStructReference = imGuiMethodsStruct.AsEntityReference();
                    model.Children.Add(imGuiMethodsStructReference);

                    ownerStruct = new EntityReferenceWrapper<Struct>(imGuiMethodsStructReference);
                    GenerateManagedMethodsForStruct(ownerStruct, freeFunctionsByNamespace.ToArray());
                }
            } else {
                GenerateManagedMethodsForStruct(ownerStruct, structFunctions);
            }
        }

        return model;
    }

    private void GenerateManagedMethodsForStruct(
        EntityReferenceWrapper<Struct> structureReference,
        EntityReferenceWrapper<Function>[] structFunctionReferences
    ) {
        Struct structure = structureReference.Value;

        // C doesn't support function overloads,
        // so overloads are implemented by appending prefixes to the base function name.
        // C# supports overloads just fine, so we need to group them back under a single name.
        IEnumerable<IGrouping<string?, EntityReferenceWrapper<Function>>> functionsByOriginalNames =
            structFunctionReferences.GroupBy(x => x.Value.OriginalFullyQualifiedName);

        foreach (IGrouping<string, EntityReferenceWrapper<Function>> functionsByOriginalName in functionsByOriginalNames) {
            EntityReferenceWrapper<Function>[] functions = functionsByOriginalName.ToArray();
            if (functions.Length == 0)
                continue;

            // Assume that the function with the shortest name is the one matching the original
            EntityReferenceWrapper<Function> originalFunction = functions.MinBy(x => x.Value.Name.Length)!;

            Function[] newFunctions =
                functions
                    .SelectMany(function => GenerateManagedMethodsForNativeFunction(structure, function.Value, originalFunction.Value.Name))
                    .ToArray();

            structure.Children.AddRange(newFunctions);
        }
    }

    private List<Function> GenerateManagedMethodsForNativeFunction(
        Struct structure,
        Function nativeFunction,
        string nativeFunctionBaseName
    ) {
        if (nativeFunction.IsDisabled)
            return [];

        BindingsFileCustomizations bindingsFileCustomizations =
            bindingsContext.Customizations
                .RequireCustomization(nativeFunction.SourceLocation);

        if (bindingsFileCustomizations.NoEmitManagedMethodsForNativeFunctions.Contains(nativeFunction.Name))
            return [];

        if (nativeFunctionBaseName.Contains("SetTexID")) {
            Console.Write((5));
        }

        bool isInstanceMethod = nativeFunction.OwnerStruct != null;

        string methodName = nativeFunctionBaseName;
        if (isInstanceMethod) {
            methodName = methodName
                .RemovePrefix(structure.Name + "_")
                .FirstCharToUpperCase();
        } else {
            bool isFreeFunction = nativeFunctionBaseName.StartsWith("cIm", StringComparison.Ordinal);
            if (!isFreeFunction) {
                methodName = methodName
                    .RemovePrefix("ImGui_")
                    .FirstCharToUpperCase();
            }
        }

        foreach (string removedPrefix in bindingsFileCustomizations.PrefixesRemovedFromManagedMethods) {
            methodName = methodName.RemovePrefix(removedPrefix);
        }

        List<NativeFunctionArgumentDescription> nativeArgumentDescriptions = [];
        for (int index = 0; index < nativeFunction.Arguments.Count; index++) {
            FunctionArgument nativeFunctionArgument = nativeFunction.Arguments[index];
            bool isString = IsTypeConstCharPointer(nativeFunctionArgument.Type);

            nativeArgumentDescriptions.Add(
                new NativeFunctionArgumentDescription(nativeFunctionArgument, index) {
                    IsString = isString,
                    IsPointer = !isString && nativeFunctionArgument.Type.ExpandTypedef()!.Value is Pointer
                }
            );
        }

        bool isFunctionBanned =
            bindingsFileCustomizations
                .FunctionsNoPermutations
                .Contains(nativeFunctionBaseName);

        NativeFunctionArgumentMarshalingVariant[][] nativeArgumentMarshalingVariants =
            nativeArgumentDescriptions
                .Select(arg => ResolveNativeArgumentMarshalingVariants(arg, isFunctionBanned))
                .ToArray();

        List<MethodOverloadRaw> rawMethodOverloads = [];
        GenerateArgumentMarshalingVariantsMethodOverloads(nativeArgumentMarshalingVariants, rawMethodOverloads, null, 0, 0);

        List<MethodSignature> methodSignatures =
            rawMethodOverloads
                .Select(raw =>
                    new MethodSignature(
                        nativeFunction,
                        ResolveManagedMethodArguments(raw.Arguments).ToEqualityList()
                    )
                )
                .ToList();

        if (methodSignatures.Count == 0) {
            methodSignatures.Add(new MethodSignature(nativeFunction, []));
        }

        Range? defaultValueInvalidArgumentsRange =
            GetInvalidForSignatureDefaultValuesArgumentIndexRange(methodSignatures);

        if (defaultValueInvalidArgumentsRange != null) {
            methodSignatures =
                methodSignatures
                    .SelectMany(methodSignature =>
                        GenerateDefaultValueMethodOverloads(
                            methodSignature,
                            defaultValueInvalidArgumentsRange.Value.Start.Value,
                            defaultValueInvalidArgumentsRange.Value.End.Value
                        )
                    )
                    .DistinctBy(MethodSignatureArgumentsDiscriminator.Create)
                    .ToList();
        }

#if DEBUG
        if (Log.IsEnabled(LogLevel.Trace) && methodSignatures.Count > 3) {
            Log.LogTrace($"==== Overloads for {nativeFunction.Name}");
            for (int methodSignatureIndex = 0; methodSignatureIndex < methodSignatures.Count; methodSignatureIndex++) {
                Log.LogTrace(FormatMethodSignatureForOverloadDebug(nativeFunction.Name, methodSignatures, methodSignatureIndex));
            }
        }
#endif

        string nativeFunctionsClassName = bindingsFileCustomizations.NativeFunctionsClassName;

        List<Function> managedMethods = [];
        foreach (MethodSignature methodSignature in methodSignatures) {
            managedMethods.Add(
                GenerateManagedMethodForNativeFunction(
                    structure,
                    methodName,
                    methodSignature,
                    nativeFunctionsClassName
                )
            );
        }

        return managedMethods;
    }

    private static Struct CreateImGuiManagedMethodsStruct(string targetNamespace, string structName) {
        Struct structure = new() {
            Kind = Struct.StructKind.StaticClass,
            Name = structName,
            Namespace = targetNamespace,
            Comments = new Comments(["Dear ImGui managed methods"], [], []),
            IsSpecialTopLevel = true
        };

        return structure;
    }

    private static Range? GetInvalidForSignatureDefaultValuesArgumentIndexRange(List<MethodSignature> methodSignatures) {
        if (methodSignatures.Count == 0)
            return null;

        int argumentsCount = methodSignatures[0].Arguments.Count;

        int? firstArgumentInvalidInAnySignature = null;
        int? lastArgumentInvalidInAnySignature = null;

        for (int argIndex = 0; argIndex < argumentsCount; argIndex++) {
            foreach (MethodSignature signature in methodSignatures) {
                if (!signature.Arguments[argIndex].MarshaledArgument.IsDefaultValueValidInDeclaration) {
                    firstArgumentInvalidInAnySignature ??= argIndex;
                    lastArgumentInvalidInAnySignature = argIndex;
                }
            }
        }

        if (firstArgumentInvalidInAnySignature == null || lastArgumentInvalidInAnySignature == null)
            return null;

        return new Range(firstArgumentInvalidInAnySignature.Value, lastArgumentInvalidInAnySignature.Value);
    }

    private static Function GenerateManagedMethodForNativeFunction(
        Struct structure,
        string methodName,
        MethodSignature methodSignature,
        string nativeFunctionsClassName
    ) {
        bool isInstanceMethod = methodSignature.NativeFunction.OwnerStruct != null;

        bool hasReturnValue =
            methodSignature.NativeFunction.ReturnType.ExpandTypedef()?.Value is not Builtin builtin ||
            builtin.ManagedType != typeof(void);

        string retValVar = InternalVariableName("retVal");

        IArgumentMarshaller[] argumentMarshallers =
            methodSignature.Arguments
                .Select(arg => ArgumentMarshallerFactory.Create(methodSignature, arg))
                .ToArray();

        List<StatementSyntax> statements = [];

        FormatTypeOptions varTypeFormatOptions = new() {
            ExpandTypedefs = false,
        };

        void Append(ManagedMethodArgumentMarshallerStatement newStatement) {
            newStatement.Switch(
                statementText => statements.Add(ParseStatementWithLeadingLineFeed(statementText)),
                statementSyntax => statements.Add(statementSyntax),
                syntaxTrivia => {
                    if (statements.Count != 0) {
                        statements[^1] = statements[^1].AddTrailingTriviaToEnd(syntaxTrivia);
                    }
                });
        }

        void AppendRange(IReadOnlyList<ManagedMethodArgumentMarshallerStatement> newStatements) {
            foreach (ManagedMethodArgumentMarshallerStatement newStatement in newStatements) {
                Append(newStatement);
            }
        }

        foreach (IArgumentMarshaller argumentMarshaller in argumentMarshallers) {
            AppendRange(argumentMarshaller.CreateMarshalStatements());
        }

        if (hasReturnValue) {
            Append($"{FormatType(methodSignature.NativeFunction.ReturnType.Value, varTypeFormatOptions)} {retValVar};");
        }

        string retValAssignment = hasReturnValue ? $"{retValVar} = " : "";
        string invocationValuesFormatted = String.Join(", ", argumentMarshallers.Select(x => x.InvocationVariableName));
        Append($"{retValAssignment}{nativeFunctionsClassName}.{methodSignature.NativeFunction.Name}({invocationValuesFormatted});");

        foreach (IArgumentMarshaller argumentMarshaller in argumentMarshallers) {
            AppendRange(argumentMarshaller.CreateUnmarshalStatements());
        }

        if (hasReturnValue) {
            Append($"return {retValVar};");
        }

        List<FunctionArgument> signatureArguments =
            methodSignature.Arguments
                .Where(x => x.IsPartOfDeclaration)
                .Select(x => x.MarshaledArgument)
                .ToList();

        Function managedMethod = methodSignature.NativeFunction with {
            Name = methodName,
            Body = Block(statements),
            Arguments = signatureArguments,
            IsNativeFunction = false
        };

        return managedMethod;

        static string InternalVariableName(string name) {
            return "__" + name;
        }
    }

    private static List<MethodSignature> GenerateDefaultValueMethodOverloads(
        MethodSignature baseMethod,
        int firstInvalidArgument,
        int lastInvalidArgument
    ) {
        List<MethodSignature> result = [];

        ManagedMethodArgument[] baseArguments = baseMethod.Arguments.ToArray();
        DisableDefaultValuesInArguments(baseArguments, lastInvalidArgument);

        MethodSignature baseOverload = baseMethod with {
            Arguments = baseArguments
        };

        result.Add(baseOverload);

        for (int baseArgIndex = firstInvalidArgument; baseArgIndex <= lastInvalidArgument; baseArgIndex++) {
            ManagedMethodArgument[] overloadArguments = baseArguments.ToArray();

            DisableDefaultValuesInArguments(overloadArguments, lastInvalidArgument);
            RemoveInvalidArgumentsFromDeclaration(overloadArguments, firstInvalidArgument);

            MethodSignature overload = baseMethod with {
                Arguments = overloadArguments
            };

            result.Add(overload);
        }

        return result;

        static void DisableDefaultValuesInArguments(IList<ManagedMethodArgument> overloadArguments, int lastInvalidArgument) {
            for (int modifiedArgIndex = 0; modifiedArgIndex <= lastInvalidArgument; modifiedArgIndex++) {
                overloadArguments[modifiedArgIndex] = overloadArguments[modifiedArgIndex] with {
                    MarshaledArgument = overloadArguments[modifiedArgIndex].MarshaledArgument with {
                        IsDefaultValueValidInDeclaration = false
                    }
                };
            }
        }

        static void RemoveInvalidArgumentsFromDeclaration(IList<ManagedMethodArgument> overloadArguments, int firstInvalidArgument) {
            for (int modifiedArgIndex = firstInvalidArgument; modifiedArgIndex < overloadArguments.Count; modifiedArgIndex++) {
                overloadArguments[modifiedArgIndex] = overloadArguments[modifiedArgIndex] with {
                    IsPartOfDeclaration = false
                };
            }
        }
    }

    private static void GenerateArgumentMarshalingVariantsMethodOverloads(
        IReadOnlyList<IReadOnlyList<NativeFunctionArgumentMarshalingVariant>> argumentsVariants,
        List<MethodOverloadRaw> outputMethodOverloads,
        NativeFunctionArgumentMarshalingVariant[]? currentMethodOverload,
        int argumentIndex,
        int depth
    ) {
        if (argumentIndex == argumentsVariants.Count)
            return;

        currentMethodOverload ??= new NativeFunctionArgumentMarshalingVariant[argumentsVariants.Count];
        IReadOnlyList<NativeFunctionArgumentMarshalingVariant> argumentVariants = argumentsVariants[argumentIndex];
        foreach (NativeFunctionArgumentMarshalingVariant argumentVariant in argumentVariants) {
            currentMethodOverload[argumentIndex] = argumentVariant;
            GenerateArgumentMarshalingVariantsMethodOverloads(
                argumentsVariants,
                outputMethodOverloads,
                currentMethodOverload,
                argumentIndex + 1,
                depth + 1
            );

            if (depth == argumentsVariants.Count - 1) {
                outputMethodOverloads.Add(
                    new MethodOverloadRaw(currentMethodOverload.ToArray())
                );
            }
        }
    }

    private static NativeFunctionArgumentMarshalingVariant[] ResolveNativeArgumentMarshalingVariants(
        NativeFunctionArgumentDescription description,
        bool forceBaseOnly
    ) {
        NativeFunctionArgumentMarshalingVariant baseMarshalingVariant = new(
            description.Argument,
            description.ArgumentIndex,
            description.IsString ? StringMarshalingMode.BytePointer : StringMarshalingMode.Undefined,
            description.IsPointer ? PointerMarshalMode.Pointer : PointerMarshalMode.Undefined
        );

        if (description.Argument.IsInstancePointer || forceBaseOnly)
            return [baseMarshalingVariant];

        List<NativeFunctionArgumentMarshalingVariant> variants = [];
        if (description.IsString) {
            AddVariant(StringMarshalingMode.ReadOnlyCharSpan, PointerMarshalMode.Undefined);
            AddVariant(StringMarshalingMode.ReadOnlyByteSpan, PointerMarshalMode.Undefined);
            AddVariant(StringMarshalingMode.String, PointerMarshalMode.Undefined);

            // FIXME: handle combination with IsPointer
        } else if (description.IsPointer) {
            AddVariant(StringMarshalingMode.Undefined, PointerMarshalMode.Pointer);

            bool isOutPointer = description.Argument.Name.Contains("out", StringComparison.OrdinalIgnoreCase);
            bool isVoidPointer = description.Argument.Type.Value.IsVoidPointer();
            bool isVoidPointerPointer =
                description.Argument.Type.Value is Pointer pointer &&
                pointer.ValueType.Value.IsVoidPointer();

            if (isVoidPointerPointer) {
                AddVariant(StringMarshalingMode.Undefined, PointerMarshalMode.IntPtr);
                if (isOutPointer) {
                    AddVariant(StringMarshalingMode.Undefined, PointerMarshalMode.Out);
                    AddVariant(StringMarshalingMode.Undefined, PointerMarshalMode.IntPtrOut);
                } else {
                    AddVariant(StringMarshalingMode.Undefined, PointerMarshalMode.Ref);
                    AddVariant(StringMarshalingMode.Undefined, PointerMarshalMode.IntPtrRef);
                }
            } else if (isVoidPointer) {
                AddVariant(StringMarshalingMode.Undefined, PointerMarshalMode.IntPtr);
            } else {
                if (isOutPointer) {
                    AddVariant(StringMarshalingMode.Undefined, PointerMarshalMode.Out);
                } else {
                    AddVariant(StringMarshalingMode.Undefined, PointerMarshalMode.Ref);
                }
            }
        }

        if (variants.Count == 0) {
            variants.Add(baseMarshalingVariant);
        }

        return variants.ToArray();

        void AddVariant(StringMarshalingMode stringMarshalKind, PointerMarshalMode pointerMarshalKind) {
            variants.Add(
                new NativeFunctionArgumentMarshalingVariant(
                    description.Argument,
                    description.ArgumentIndex,
                    stringMarshalKind,
                    pointerMarshalKind
                )
            );
        }
    }

    private static List<ManagedMethodArgument> ResolveManagedMethodArguments(
        IReadOnlyList<NativeFunctionArgumentMarshalingVariant> nativeArgumentMarshalingVariants
    ) {
        List<ManagedMethodArgument> managedArguments = [];
        foreach (NativeFunctionArgumentMarshalingVariant nativeArgumentMarshalingVariant in nativeArgumentMarshalingVariants) {
            FunctionArgument managedArgument = nativeArgumentMarshalingVariant.NativeFunctionArgument with { };
            managedArgument = FunctionModifiers.PrettifyArgumentName(managedArgument);

            switch (nativeArgumentMarshalingVariant.StringMarshalingMode) {
                case StringMarshalingMode.BytePointer:
                    managedArgument.Type = KnownEntities.Utf8BytePointer.AsEntityReference();
                    break;
                case StringMarshalingMode.ReadOnlyByteSpan:
                    managedArgument.Type = KnownEntities.ReadOnlySpanByte.AsEntityReference();
                    break;
                case StringMarshalingMode.ReadOnlyCharSpan:
                    managedArgument.Type = KnownEntities.ReadOnlySpanChar.AsEntityReference();
                    break;
                case StringMarshalingMode.String:
                    managedArgument.Type = KnownEntities.String.AsEntityReference();
                    break;
                case StringMarshalingMode.Undefined:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            switch (nativeArgumentMarshalingVariant.PointerMarshalMode) {
                case PointerMarshalMode.Undefined:
                case PointerMarshalMode.Pointer:
                    break;
                case PointerMarshalMode.Ref:
                    managedArgument.PassType = FunctionArgumentPassType.Ref;
                    managedArgument.Type = managedArgument.Type.ExpandPointer();
                    managedArgument.IsDefaultValueValidInDeclaration = false;

                    break;
                case PointerMarshalMode.Out:
                    managedArgument.PassType = FunctionArgumentPassType.Out;
                    managedArgument.Type = managedArgument.Type.ExpandPointer();
                    managedArgument.IsDefaultValueValidInDeclaration = false;

                    break;
                case PointerMarshalMode.IntPtr:
                    managedArgument.PassType = FunctionArgumentPassType.Normal;
                    managedArgument.Type = KnownEntities.IntPtr.AsEntityReference();

                    break;
                case PointerMarshalMode.IntPtrRef:
                    managedArgument.PassType = FunctionArgumentPassType.Ref;
                    managedArgument.Type = KnownEntities.IntPtr.AsEntityReference();
                    managedArgument.IsDefaultValueValidInDeclaration = false;

                    break;
                case PointerMarshalMode.IntPtrOut:
                    managedArgument.PassType = FunctionArgumentPassType.Out;
                    managedArgument.Type = KnownEntities.IntPtr.AsEntityReference();
                    managedArgument.IsDefaultValueValidInDeclaration = false;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            managedArguments.Add(
                new ManagedMethodArgument(
                    nativeArgumentMarshalingVariant.NativeFunctionArgument,
                    managedArgument,
                    nativeArgumentMarshalingVariant,
                    !nativeArgumentMarshalingVariant.NativeFunctionArgument.IsInstancePointer
                )
            );
        }

        return managedArguments;
    }

    private static bool IsTypeConstCharPointer(IEntityReference type) =>
        type.ExpandTypedef()!.Value is Pointer {
            ValueType.Value: Builtin { IsConstCharPointer: true }
        };

    private static string FormatMethodSignatureForOverloadDebug(
        string nativeFunctionName,
        List<MethodSignature> methodSignatures,
        int index
    ) {
        MethodSignature methodSignature = methodSignatures[index];

        List<string> argumentDescriptions = [];
        for (int argumentIndex = 0; argumentIndex < methodSignature.Arguments.Count; argumentIndex++) {
            ManagedMethodArgument argument = methodSignature.Arguments[argumentIndex];
            bool argumentIsSameInAllOverloads =
                methodSignatures.All(x => x.Arguments[argumentIndex] == argument);

            string argumentDescription = $"[{argumentIndex + 1}]";
            if (!argumentIsSameInAllOverloads) {
                argumentDescription += $" {argument.MarshalingVariant}";
            }

            if (!argument.IsPartOfDeclaration) {
                argumentDescription += " (def-only)";
            }

            argumentDescriptions.Add(argumentDescription);
        }

        return $"{nativeFunctionName} [variant {index + 1}]: ({String.Join(", ", argumentDescriptions)})";
    }

    private record MethodOverloadRaw(IReadOnlyList<NativeFunctionArgumentMarshalingVariant> Arguments);

    private readonly record struct NativeFunctionArgumentDescription(FunctionArgument Argument, int ArgumentIndex) {
        public bool IsString { get; init; }
        public bool IsPointer { get; init; }
    }
}

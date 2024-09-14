using System.Diagnostics;
using CaseConverter;
using Microsoft.CodeAnalysis;
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
                IGrouping<string, EntityReferenceWrapper<Function>>[] allFreeFunctionsByNamespaces =
                    structFunctions
                        .GroupBy(x => x.Value.Namespace!)
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
                    GenerateManagedMethodsForStruct(ownerStruct.Value, true, freeFunctionsByNamespace.ToArray());
                }
            } else {
                GenerateManagedMethodsForStruct(ownerStruct.Value, false, structFunctions);
            }
        }

        return model;
    }

    private void GenerateManagedMethodsForStruct(
        Struct structure,
        bool isFreeFunctionsWrapperStruct,
        EntityReferenceWrapper<Function>[] structFunctionReferences
    ) {
        // C doesn't support function overloads,
        // so overloads are implemented by appending prefixes to the base function name.
        // C# supports overloads just fine, so we need to group them back under a single name.
        IEnumerable<IGrouping<string?, EntityReferenceWrapper<Function>>> functionsByOriginalNames =
            structFunctionReferences.GroupBy(x => x.Value.OriginalFullyQualifiedName);

        foreach (IGrouping<string?, EntityReferenceWrapper<Function>> functionsByOriginalName in functionsByOriginalNames) {
            EntityReferenceWrapper<Function>[] functions = functionsByOriginalName.ToArray();
            if (functions.Length == 0)
                continue;

            BindingsFileCustomizations bindingsFileCustomizations =
                bindingsContext.Customizations
                    .RequireCustomization(functions[0].Value.SourceLocation);

            Function[] newFunctions =
                functions
                    .SelectMany(function => {
                        (string nativeFunctionBaseName, string managedMethodName) =
                            CalculateManagedMethodName(
                                isFreeFunctionsWrapperStruct ? "ImGui" : structure.Name,
                                functions,
                                function.Value,
                                bindingsFileCustomizations
                            );

                        return GenerateManagedMethodsForNativeFunction(
                            structure,
                            function.Value,
                            nativeFunctionBaseName,
                            managedMethodName,
                            bindingsFileCustomizations
                        );
                    })
                    .ToArray();

            structure.Children.AddRange(newFunctions);
        }
    }

    private static(string nativeFunctionBaseName, string managedMethodName) CalculateManagedMethodName(
        string structureNativeName,
        IReadOnlyList<EntityReferenceWrapper<Function>> nativeFunctions,
        Function nativeFunction,
        BindingsFileCustomizations bindingsFileCustomizations
    ) {
        bool isInstanceMethod = nativeFunction.OwnerStruct != null;

        // Assume that the function with the shortest name is the one matching the original
        EntityReferenceWrapper<Function> originalFunction = nativeFunctions.MinBy(x => x.Value.Name.Length)!;
        string nativeFunctionBaseName = originalFunction.Value.Name;

        // Sometimes all we get is functions with added suffixes.
        // Try to recover the actual original name
        if (!String.IsNullOrEmpty(originalFunction.Value.OriginalFullyQualifiedName)) {
            int classSeparatorIndex =
                originalFunction.Value.OriginalFullyQualifiedName.IndexOf("::", StringComparison.Ordinal);

            if (classSeparatorIndex != -1) {
                string potentialBaseFunctionName =
                    structureNativeName +
                    "_" +
                    originalFunction.Value.OriginalFullyQualifiedName[(classSeparatorIndex + 2)..];

                if (potentialBaseFunctionName.Length < nativeFunctionBaseName.Length) {
                    nativeFunctionBaseName = potentialBaseFunctionName;
                }
            }
        }

        // Unformatted is the only prefix we preserve
        if (nativeFunction.IsUnformattedHelper) {
            nativeFunctionBaseName = nativeFunction.Name;
        }

        // Figure out the managed method name
        string managedMethodName = nativeFunctionBaseName;
        if (isInstanceMethod) {
            managedMethodName = managedMethodName
                .RemovePrefix(structureNativeName + "_")
                .FirstCharToUpperCase();
        } else {
            bool isFreeFunction = nativeFunctionBaseName.StartsWith("cIm", StringComparison.Ordinal);
            if (!isFreeFunction) {
                managedMethodName = managedMethodName
                    .RemovePrefix("ImGui_")
                    .FirstCharToUpperCase();
            }
        }

        foreach (string removedPrefix in bindingsFileCustomizations.PrefixesRemovedFromManagedMethods) {
            managedMethodName = managedMethodName.RemovePrefix(removedPrefix);
        }

        return (nativeFunctionBaseName, managedMethodName);
    }

    private List<Function> GenerateManagedMethodsForNativeFunction(
        Struct structure,
        Function nativeFunction,
        string nativeFunctionBaseName,
        string managedMethodName,
        BindingsFileCustomizations bindingsFileCustomizations
    ) {
        if (nativeFunction.IsDisabled)
            return [];

        if (bindingsFileCustomizations.NoEmitManagedMethodsForNativeFunctions.Contains(nativeFunction.Name))
            return [];

        // Collect metadata about function argument and return types
        List<NativeFunctionArgumentDescription> nativeArgumentDescriptions = [];
        for (int index = 0; index < nativeFunction.Arguments.Count; index++) {
            FunctionArgument nativeFunctionArgument = nativeFunction.Arguments[index];
            bool isString = nativeFunctionArgument.Type.IsConstCharPointer();

            nativeArgumentDescriptions.Add(
                new NativeFunctionArgumentDescription(nativeFunctionArgument, index) {
                    IsString = isString,
                    IsPointer = !isString && nativeFunctionArgument.Type.ExpandTypedef().Value is Pointer,
                    IsDelegate = nativeFunctionArgument.Type.ExpandAllTypedefsAndPointers().Value is Delegate,
                }
            );
        }

        NativeFunctionReturnTypeDescription nativeFunctionReturnTypeDescription =
            new(nativeFunction.ReturnType.Value) {
                IsString = nativeFunction.ReturnType.Value.IsConstCharPointer(),
                IsVoid = nativeFunction.ReturnType.Value.IsVoid()
            };

        // Generate marshalling variant of return types and of every argument

        bool isFunctionBanned =
            bindingsFileCustomizations
                .FunctionsNoPermutations
                .Contains(nativeFunctionBaseName);

        NativeFunctionArgumentMarshallingVariant[][] nativeArgumentMarshallingVariants =
            nativeArgumentDescriptions
                .Select((_, argIndex) => ResolveNativeArgumentMarshallingVariants(nativeArgumentDescriptions, argIndex, isFunctionBanned))
                .ToArray();

        NativeFunctionReturnTypeMarshallingVariant[] nativeReturnTypeMarshallingVariants =
            ResolveNativeReturnTypeMarshallingVariants(nativeFunctionReturnTypeDescription);

        // FIXME: account for more than one return type variant
        Debug.Assert(nativeReturnTypeMarshallingVariants.Length == 1);

        List<MethodArgumentOverloadRaw> rawMethodArgumentOverloads = [];
        GenerateArgumentMarshallingVariantsMethodOverloads(nativeArgumentMarshallingVariants, rawMethodArgumentOverloads, null, 0, 0);

        rawMethodArgumentOverloads = FilterRawMethodArgumentOverloads(rawMethodArgumentOverloads);

        List<MethodSignature> methodSignatures =
            rawMethodArgumentOverloads
                .Select(raw =>
                    new MethodSignature(
                        nativeFunction,
                        ResolveManagedMethodArguments(raw.Arguments).ToEqualityList(),
                        ResolveManagedMethodReturnType(nativeReturnTypeMarshallingVariants[0])
                    )
                )
                .ToList();

        if (methodSignatures.Count == 0) {
            methodSignatures.Add(
                new MethodSignature(
                    nativeFunction,
                    [],
                    ResolveManagedMethodReturnType(nativeReturnTypeMarshallingVariants[0])
                )
            );
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

        // Fewest arguments first
        methodSignatures =
            methodSignatures
                .OrderBy(x => x.Arguments.Count(arg => arg.IsPartOfDeclaration))
                .ToList();

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
                    managedMethodName,
                    methodSignature,
                    nativeFunctionsClassName
                )
            );
        }

        BindingsFileCustomizations.ManagedMethodAliasDefinition[] aliases =
            bindingsFileCustomizations.GenerateManagedMethodAliases
                .Where(x => x.NativeFunction == nativeFunction.Name)
                .ToArray();

        List<Function> managedMethodAliases = [];
        foreach (BindingsFileCustomizations.ManagedMethodAliasDefinition alias in aliases) {
            foreach (Function managedMethod in managedMethods) {
                managedMethodAliases.Add(
                    GenerateManagedMethodAlias(
                        managedMethod,
                        alias.ManagedMethodAlias,
                        nativeFunction,
                        nativeFunctionsClassName
                    )
                );
            }
        }

        managedMethods.AddRange(managedMethodAliases);

        return managedMethods;
    }

    private List<MethodArgumentOverloadRaw> FilterRawMethodArgumentOverloads(List<MethodArgumentOverloadRaw> overloads) {
        List<MethodArgumentOverloadRaw> newOverloads = [];
        foreach (MethodArgumentOverloadRaw overload in overloads) {
            (int Index, NativeFunctionArgumentMarshallingVariant Item) stringBeginEndPairBeginWithIndex =
                overload.Arguments
                    .Index()
                    .FirstOrDefault(
                        argWithIndex =>
                            argWithIndex.Item.StringBeginEndPairMode is
                                StringBeginEndPairMode.Begin or
                                StringBeginEndPairMode.BeginOnly
                    );

            NativeFunctionArgumentMarshallingVariant stringBeginEndPairBegin = stringBeginEndPairBeginWithIndex.Item;
            if (stringBeginEndPairBegin != null) {
                NativeFunctionArgumentMarshallingVariant stringBeginEndPairEnd =
                    overload.Arguments[stringBeginEndPairBeginWithIndex.Index + 1];

                bool isValidStringBeginEndPair =
                    (
                        stringBeginEndPairBegin.StringBeginEndPairMode == StringBeginEndPairMode.Begin &&
                        stringBeginEndPairEnd.StringBeginEndPairMode == StringBeginEndPairMode.End
                    )
                    ||
                    (
                        stringBeginEndPairBegin.StringBeginEndPairMode == StringBeginEndPairMode.BeginOnly &&
                        stringBeginEndPairEnd.StringBeginEndPairMode == StringBeginEndPairMode.EndDefault
                    );

                if (!isValidStringBeginEndPair)
                    continue;
            }

            newOverloads.Add(overload);
        }

        return newOverloads;
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
                if (!signature.Arguments[argIndex].MarshalledArgument.IsDefaultValueValidInDeclaration) {
                    firstArgumentInvalidInAnySignature ??= argIndex;
                    lastArgumentInvalidInAnySignature = argIndex;
                }
            }
        }

        if (firstArgumentInvalidInAnySignature == null || lastArgumentInvalidInAnySignature == null)
            return null;

        return new Range(firstArgumentInvalidInAnySignature.Value, lastArgumentInvalidInAnySignature.Value);
    }

    private static Function GenerateManagedMethodAlias(
        Function managedMethod,
        string aliasMethodName,
        Function nativeFunction,
        string nativeFunctionsClassName
    ) {
        string invocationValuesFormatted =
            String.Join(", ", managedMethod.Arguments.Select(x => x.Name));

        string invocation;
        if (managedMethod.IsConstructor) {
            invocation =
                $"{nativeFunctionsClassName}.{nativeFunction.Name}({invocationValuesFormatted})";
        } else {
            invocation =
                $"{managedMethod.Name}({invocationValuesFormatted})";
        }

        Function aliasMethod = managedMethod with {
            Name = aliasMethodName,
            Body = ArrowExpressionClause(ParseExpression(invocation)),
            IsConstructor = false,
            IsStatic = managedMethod.IsConstructor
        };

        return aliasMethod;
    }

    private static Function GenerateManagedMethodForNativeFunction(
        Struct structure,
        string methodName,
        MethodSignature methodSignature,
        string nativeFunctionsClassName
    ) {
        IArgumentMarshaller[] argumentMarshallers =
            methodSignature.Arguments
                .Select(arg => ArgumentMarshallerFactory.Create(methodSignature, arg))
                .ToArray();

        string invocationValuesFormatted =
            String.Join(", ", argumentMarshallers.Select(x => x.InvocationVariableName));
        string nativeFunctionInvocation =
            $"{nativeFunctionsClassName}.{methodSignature.NativeFunction.Name}({invocationValuesFormatted})";

        IReturnTypeMarshaller returnTypeMarshaller =
            ReturnTypeMarshallerFactory.Create(methodSignature, nativeFunctionInvocation);

        StatementSyntaxListBuilder bodyBuilder = new();

        foreach (IArgumentMarshaller argumentMarshaller in argumentMarshallers) {
            bodyBuilder.AppendRange(argumentMarshaller.CreateMarshalStatements());
        }

        bodyBuilder.AppendRange(returnTypeMarshaller.CreateInvokeStatements());

        foreach (IArgumentMarshaller argumentMarshaller in argumentMarshallers) {
            bodyBuilder.AppendRange(argumentMarshaller.CreateUnmarshalStatements());
        }

        bodyBuilder.AppendRange(returnTypeMarshaller.CreateReturnStatements());

        List<FunctionArgument> signatureArguments =
            methodSignature.Arguments
                .Where(x => x.IsPartOfDeclaration)
                .Select(x => x.MarshalledArgument)
                .ToList();

        Function managedMethod = methodSignature.NativeFunction with {
            Name = methodName,
            Body = Block(bodyBuilder.Statements),
            Arguments = signatureArguments,
            ReturnType = methodSignature.ReturnType.MarshalledReturnType.AsEntityReference(),
            IsNativeFunction = false
        };

        // Only enable String arguments on .NET Standard 2.0,
        // as newer NET versions can just use ReadOnlySpan<char>

        bool anyStringsInSignatureArguments =
            signatureArguments
                .Any(arg =>
                    arg.Type.ExpandPointer().Value is Builtin builtin &&
                    builtin.ManagedType == typeof(string)
                );

        if (anyStringsInSignatureArguments) {
            managedMethod.Conditionals = [
                ..managedMethod.Conditionals,
                new Conditional("NETSTANDARD2_0")
            ];
        }

        // Only enable function pointer arguments on .NET 6.0+,
        // as older versions don't support function pointer with
        // unmanaged calling conventions
        bool anyFunctionPointerSignatureArguments =
            signatureArguments
                .Any(arg =>
                    arg.Type.ExpandTypedef().Value is Delegate { IsFunctionPointer: true }
                );

        if (anyFunctionPointerSignatureArguments) {
            managedMethod.Conditionals = [
                ..managedMethod.Conditionals,
                new Conditional("NET6_0_OR_GREATER")
            ];
        }

        return managedMethod;
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
            RemoveInvalidArgumentsFromDeclaration(overloadArguments, baseArgIndex);

            MethodSignature overload = baseMethod with {
                Arguments = overloadArguments
            };

            result.Add(overload);
        }

        return result;

        static void DisableDefaultValuesInArguments(IList<ManagedMethodArgument> overloadArguments, int lastInvalidArgument) {
            for (int modifiedArgIndex = 0; modifiedArgIndex <= lastInvalidArgument; modifiedArgIndex++) {
                overloadArguments[modifiedArgIndex] = overloadArguments[modifiedArgIndex] with {
                    MarshalledArgument = overloadArguments[modifiedArgIndex].MarshalledArgument with {
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

    private static void GenerateArgumentMarshallingVariantsMethodOverloads(
        IReadOnlyList<IReadOnlyList<NativeFunctionArgumentMarshallingVariant>> argumentsVariants,
        List<MethodArgumentOverloadRaw> outputMethodOverloads,
        NativeFunctionArgumentMarshallingVariant[]? currentMethodOverload,
        int argumentIndex,
        int depth
    ) {
        if (argumentIndex == argumentsVariants.Count)
            return;

        currentMethodOverload ??= new NativeFunctionArgumentMarshallingVariant[argumentsVariants.Count];
        IReadOnlyList<NativeFunctionArgumentMarshallingVariant> argumentVariants = argumentsVariants[argumentIndex];
        foreach (NativeFunctionArgumentMarshallingVariant argumentVariant in argumentVariants) {
            currentMethodOverload[argumentIndex] = argumentVariant;
            GenerateArgumentMarshallingVariantsMethodOverloads(
                argumentsVariants,
                outputMethodOverloads,
                currentMethodOverload,
                argumentIndex + 1,
                depth + 1
            );

            if (depth == argumentsVariants.Count - 1) {
                outputMethodOverloads.Add(
                    new MethodArgumentOverloadRaw(currentMethodOverload.ToArray())
                );
            }
        }
    }

    private static NativeFunctionReturnTypeMarshallingVariant[] ResolveNativeReturnTypeMarshallingVariants(
        NativeFunctionReturnTypeDescription description
    ) {
        NativeFunctionReturnTypeMarshallingVariant voidVariant = new(
            description.ReturnType,
            ReturnTypeMarshallingMode.Void
        );

        if (description.IsVoid)
            return [voidVariant];

        NativeFunctionReturnTypeMarshallingVariant ntsVariant = new(
            description.ReturnType,
            ReturnTypeMarshallingMode.NullTerminatedString
        );

        if (description.IsString)
            return [ntsVariant];

        NativeFunctionReturnTypeMarshallingVariant passthroughVariant = new(
            description.ReturnType,
            ReturnTypeMarshallingMode.Passthrough
        );

        return [passthroughVariant];

        //List<NativeFunctionReturnTypeMarshallingVariant> variants = [];
        //return variants.ToArray();
    }

    private static NativeFunctionArgumentMarshallingVariant[] ResolveNativeArgumentMarshallingVariants(
        IReadOnlyList<NativeFunctionArgumentDescription> argumentDescriptions,
        int argumentIndex,
        bool forceBaseOnly
    ) {
        NativeFunctionArgumentDescription description = argumentDescriptions[argumentIndex];

        NativeFunctionArgumentMarshallingVariant baseMarshallingVariant = new(
            description.Argument,
            description.ArgumentIndex,
            ArgumentMarshallingMode.Passthrough,
            StringBeginEndPairMode.None
        );

        if (description.Argument.IsInstancePointer || forceBaseOnly)
            return [baseMarshallingVariant];

        List<NativeFunctionArgumentMarshallingVariant> variants = [];
        if (description.IsDelegate) {
            if (description.IsPointer) {
                // Assume it's an out argument (like ImGui.GetAllocatorFunctions)
                AddVariant(ArgumentMarshallingMode.IntPtrOut);
            } else {
                AddVariant(ArgumentMarshallingMode.DelegateManaged);
                AddVariant(ArgumentMarshallingMode.DelegateFunctionPointer);
                AddVariant(ArgumentMarshallingMode.DelegateIntPtr);
            }
        } else if (description.IsString) {
            bool isStringBeginEndPairBegin = IsStringBeginEndPairBegin(argumentIndex);
            bool isStringBeginEndPairEnd = IsStringBeginEndPairBegin(argumentIndex - 1);

            if (isStringBeginEndPairBegin) {
                AddVariant(ArgumentMarshallingMode.Passthrough, StringBeginEndPairMode.Begin);

                AddVariant(ArgumentMarshallingMode.ReadOnlyByteSpan, StringBeginEndPairMode.BeginOnly);
                AddVariant(ArgumentMarshallingMode.ReadOnlyCharSpan, StringBeginEndPairMode.BeginOnly);
                AddVariant(ArgumentMarshallingMode.String, StringBeginEndPairMode.BeginOnly);
            } else if (isStringBeginEndPairEnd) {
                AddVariant(ArgumentMarshallingMode.Passthrough, StringBeginEndPairMode.End);
                AddVariant(ArgumentMarshallingMode.Passthrough, StringBeginEndPairMode.EndDefault);
            } else {
                AddVariant(ArgumentMarshallingMode.ReadOnlyByteSpan);
                AddVariant(ArgumentMarshallingMode.ReadOnlyCharSpan);
                AddVariant(ArgumentMarshallingMode.String);
            }
        } else if (description.IsPointer) {
            AddVariant(ArgumentMarshallingMode.Passthrough);

            bool isOutPointer = description.Argument.Name.Contains("out", StringComparison.OrdinalIgnoreCase);
            bool isVoidPointer = description.Argument.Type.Value.IsVoidPointer();
            bool isVoidPointerPointer =
                description.Argument.Type.Value is Pointer pointer &&
                pointer.ValueType.Value.IsVoidPointer();

            if (isVoidPointerPointer) {
                AddVariant(ArgumentMarshallingMode.IntPtr);
                if (isOutPointer) {
                    AddVariant(ArgumentMarshallingMode.Out);
                    AddVariant(ArgumentMarshallingMode.IntPtrOut);
                } else {
                    AddVariant(ArgumentMarshallingMode.Ref);
                    AddVariant(ArgumentMarshallingMode.IntPtrRef);
                }
            } else if (isVoidPointer) {
                AddVariant(ArgumentMarshallingMode.IntPtr);
            } else {
                if (isOutPointer) {
                    AddVariant(ArgumentMarshallingMode.Out);
                } else {
                    AddVariant(ArgumentMarshallingMode.Ref);
                }
            }
        }

        if (variants.Count == 0) {
            variants.Add(baseMarshallingVariant);
        }

        return variants.ToArray();

        void AddVariant(
            ArgumentMarshallingMode argumentMarshallingMode,
            StringBeginEndPairMode stringBeginEndPairMode = StringBeginEndPairMode.None
        ) {
            variants.Add(
                new NativeFunctionArgumentMarshallingVariant(
                    description.Argument,
                    description.ArgumentIndex,
                    argumentMarshallingMode,
                    stringBeginEndPairMode
                )
            );
        }

        bool IsStringBeginEndPairBegin(int argumentIndex) {
            if (argumentDescriptions.Count <= argumentIndex + 1 || argumentIndex < 0)
                return false;

            NativeFunctionArgumentDescription thisArg = argumentDescriptions[argumentIndex];
            NativeFunctionArgumentDescription nextArg = argumentDescriptions[argumentIndex + 1];

            if (!thisArg.IsString || !nextArg.IsString)
                return false;

            if (thisArg.Argument.Name + "_end" == nextArg.Argument.Name)
                return true;

            if (thisArg.Argument.Name.RemoveSuffix("_begin") + "_end" == nextArg.Argument.Name)
                return true;

            return false;
        }
    }

    private static ManagedMethodReturnType ResolveManagedMethodReturnType(
        NativeFunctionReturnTypeMarshallingVariant returnTypeMarshallingVariant
    ) {
        Entity returnType = returnTypeMarshallingVariant.NativeFunctionReturnType with { };
        switch (returnTypeMarshallingVariant.ReturnTypeMarshallingMode) {
            case ReturnTypeMarshallingMode.Void:
            case ReturnTypeMarshallingMode.Passthrough:
                break;
            case ReturnTypeMarshallingMode.NullTerminatedString:
                returnType = new Builtin("NullTerminatedString", null, StorageClass.NotSpecified);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return new ManagedMethodReturnType(
            returnTypeMarshallingVariant.NativeFunctionReturnType,
            returnType,
            returnTypeMarshallingVariant
        );
    }

    private static List<ManagedMethodArgument> ResolveManagedMethodArguments(
        IReadOnlyList<NativeFunctionArgumentMarshallingVariant> nativeArgumentMarshallingVariants
    ) {
        List<ManagedMethodArgument> managedArguments = [];
        foreach (NativeFunctionArgumentMarshallingVariant nativeArgumentMarshallingVariant in nativeArgumentMarshallingVariants) {
            FunctionArgument managedArgument = nativeArgumentMarshallingVariant.NativeFunctionArgument with { };

            switch (nativeArgumentMarshallingVariant.ArgumentMarshallingMode) {
                case ArgumentMarshallingMode.Passthrough:
                    break;
                case ArgumentMarshallingMode.Ref:
                    managedArgument.PassType = FunctionArgumentPassType.Ref;
                    managedArgument.Type = managedArgument.Type.ExpandPointer();
                    managedArgument.IsDefaultValueValidInDeclaration = false;

                    break;
                case ArgumentMarshallingMode.Out:
                    managedArgument.PassType = FunctionArgumentPassType.Out;
                    managedArgument.Type = managedArgument.Type.ExpandPointer();
                    managedArgument.IsDefaultValueValidInDeclaration = false;

                    break;
                case ArgumentMarshallingMode.IntPtr:
                    managedArgument.PassType = FunctionArgumentPassType.Normal;
                    managedArgument.Type = KnownEntities.IntPtr.AsEntityReference();

                    break;
                case ArgumentMarshallingMode.IntPtrRef:
                    managedArgument.PassType = FunctionArgumentPassType.Ref;
                    managedArgument.Type = KnownEntities.IntPtr.AsEntityReference();
                    managedArgument.IsDefaultValueValidInDeclaration = false;

                    break;
                case ArgumentMarshallingMode.IntPtrOut:
                    managedArgument.PassType = FunctionArgumentPassType.Out;
                    managedArgument.Type = KnownEntities.IntPtr.AsEntityReference();
                    managedArgument.IsDefaultValueValidInDeclaration = false;

                    break;
                case ArgumentMarshallingMode.ReadOnlyByteSpan:
                    managedArgument.Type = KnownEntities.ReadOnlySpanByte.AsEntityReference();
                    break;
                case ArgumentMarshallingMode.ReadOnlyCharSpan:
                    managedArgument.Type = KnownEntities.ReadOnlySpanChar.AsEntityReference();
                    break;
                case ArgumentMarshallingMode.String:
                    managedArgument.Type = KnownEntities.String.AsEntityReference();
                    break;
                case ArgumentMarshallingMode.DelegateManaged:
                    break;
                case ArgumentMarshallingMode.DelegateFunctionPointer:
                    Delegate argumentDelegate = (Delegate) managedArgument.Type.ExpandTypedef().Value;
                    managedArgument.Type = (argumentDelegate with {
                        IsFunctionPointer = true
                    }).AsEntityReference();
                    break;
                case ArgumentMarshallingMode.DelegateIntPtr:
                    managedArgument.Type = KnownEntities.IntPtr.AsEntityReference();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(nativeArgumentMarshallingVariant),
                        $"Unexpected ArgumentMarshallingMode '{nativeArgumentMarshallingVariant.ArgumentMarshallingMode}' " +
                        $"for argument '{nativeArgumentMarshallingVariant.NativeFunctionArgument.Name}'"
                    );
            }

            bool isPartOfDeclaration =
                !nativeArgumentMarshallingVariant.NativeFunctionArgument.IsInstancePointer &&
                nativeArgumentMarshallingVariant.StringBeginEndPairMode != StringBeginEndPairMode.EndDefault;

            switch (nativeArgumentMarshallingVariant.StringBeginEndPairMode) {
                case StringBeginEndPairMode.BeginOnly:
                    managedArgument.Name = managedArgument.Name.RemoveSuffix("_begin");
                    break;
                case StringBeginEndPairMode.EndDefault:
                    managedArgument.DefaultValue = "default";
                    break;
            }

            managedArgument = FunctionModifiers.PrettifyArgumentName(managedArgument);

            managedArguments.Add(
                new ManagedMethodArgument(
                    nativeArgumentMarshallingVariant.NativeFunctionArgument,
                    managedArgument,
                    nativeArgumentMarshallingVariant,
                    isPartOfDeclaration
                )
            );
        }

        return managedArguments;
    }

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
                argumentDescription += $" {argument.MarshallingVariant}";
            }

            if (!argument.IsPartOfDeclaration) {
                argumentDescription += " (def-only)";
            }

            argumentDescriptions.Add(argumentDescription);
        }

        return $"{nativeFunctionName} [variant {index + 1}]: ({String.Join(", ", argumentDescriptions)})";
    }

    private record MethodArgumentOverloadRaw(IReadOnlyList<NativeFunctionArgumentMarshallingVariant> Arguments);

    private readonly record struct NativeFunctionArgumentDescription(FunctionArgument Argument, int ArgumentIndex) {
        public required bool IsString { get; init; }
        public required bool IsPointer { get; init; }
        public required bool IsDelegate { get; init; }
    }

    private readonly record struct NativeFunctionReturnTypeDescription(Entity ReturnType) {
        public bool IsString { get; init; }
        public bool IsVoid { get; init; }

        //public bool IsPointer { get; init; }
    }
}

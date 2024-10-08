using System.Diagnostics;
using CaseConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp.ManagedMethods;

using static BindingsModel;
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

        bool isNoEmitMethod = TextMatchUtility.IsMatchAny(
            nativeFunction.Name,
            bindingsFileCustomizations.NoEmitManagedMethodsForNativeFunctions
        );
        if (isNoEmitMethod)
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
                IsVoid = nativeFunction.ReturnType.Value.IsVoid(),
                IsPointer = nativeFunction.ReturnType.ExpandTypedef().Value is Pointer,
                IsKnownNonNull =
                    TextMatchUtility.IsMatchAny(
                        nativeFunctionBaseName,
                        bindingsFileCustomizations.TreatNativeFunctionReturnValueAsNonNull
                    )
            };

        // Generate marshalling variant of return types and of every argument

        bool isFunctionBanned =
            TextMatchUtility.IsMatchAny(nativeFunctionBaseName, bindingsFileCustomizations.FunctionsNoPermutations);

        NativeFunctionArgumentMarshallingVariant[][] nativeArgumentMarshallingVariants =
            nativeArgumentDescriptions
                .Select((_, argIndex) => ResolveNativeArgumentMarshallingVariants(nativeArgumentDescriptions, argIndex, isFunctionBanned))
                .ToArray();

        NativeFunctionReturnTypeMarshallingVariant[] nativeReturnTypeMarshallingVariants =
            ResolveNativeReturnTypeMarshallingVariants(nativeFunctionReturnTypeDescription);

        // Each method with each return type variant needs to have a different name
        Debug.Assert(
            nativeReturnTypeMarshallingVariants.DistinctBy(v => v.ManagedMethodNameSuffix).Count() ==
            nativeReturnTypeMarshallingVariants.Length
        );

        List<MethodArgumentOverloadRaw> rawMethodArgumentOverloads = [];
        GenerateArgumentMarshallingVariantsMethodOverloads(nativeArgumentMarshallingVariants, rawMethodArgumentOverloads, null, 0, 0);

        rawMethodArgumentOverloads = FilterRawMethodArgumentOverloads(rawMethodArgumentOverloads);

        List<MethodSignature> methodSignatures = [];
        foreach (NativeFunctionReturnTypeMarshallingVariant returnTypeMarshallingVariant in nativeReturnTypeMarshallingVariants) {
            if (rawMethodArgumentOverloads.Count == 0) {
                methodSignatures.Add(
                    new MethodSignature(
                        nativeFunction,
                        [],
                        ResolveManagedMethodReturnType(returnTypeMarshallingVariant)
                    )
                );
                continue;
            }

            foreach (MethodArgumentOverloadRaw rawMethodArgumentOverload in rawMethodArgumentOverloads) {
                methodSignatures.Add(
                    new MethodSignature(
                        nativeFunction,
                        ResolveManagedMethodArguments(rawMethodArgumentOverload.Arguments).ToEqualityList(),
                        ResolveManagedMethodReturnType(returnTypeMarshallingVariant)
                    )
                );
            }
        }

        DisambiguateAmbiguousDefaultArgumentValues(methodSignatures);

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
                    .DistinctBy(MethodSignatureDiscriminator.Create)
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

        string nativeFunctionsClassName = bindingsFileCustomizations.GetNativeFunctionsRawPointersClassName();

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

    private void DisambiguateAmbiguousDefaultArgumentValues(List<MethodSignature> methodSignatures) {
        if (methodSignatures.Count < 2)
            return;

        int argumentsCount = methodSignatures[0].Arguments.Count;
        List<(int argumentIndex, ManagedMethodArgument argument, bool isPrimary)> potentiallyAmbiguousArguments = [];

        for (int sigIndex = 0; sigIndex < methodSignatures.Count; sigIndex++) {
            for (int argIndex = 0; argIndex < argumentsCount; argIndex++) {
                MethodSignature signature = methodSignatures[sigIndex];
                ManagedMethodArgument argument = signature.Arguments[argIndex];

                ArgumentMarshallingMode marshallingMode = argument.MarshallingVariant.ArgumentMarshallingMode;

                // Check if the argument default value is potentially ambiguous
                if (argument.MarshalledArgument.DefaultValue == null ||
                    !KnownEntities.PotentiallyAmbiguousDefaultValues.Contains(argument.MarshalledArgument.DefaultValue)) {
                    continue;
                }

                // Handle possibly ambiguous combination and pick the argument that will not be disambiguated
                switch (marshallingMode) {
                    case ArgumentMarshallingMode.StringReadOnlyByteSpan or
                        ArgumentMarshallingMode.StringReadOnlyCharSpan or
                        ArgumentMarshallingMode.StringImGuiUtf8StringWriter or
                        ArgumentMarshallingMode.StringManaged: {
                        bool isPrimaryArgument = marshallingMode == ArgumentMarshallingMode.StringReadOnlyCharSpan;
                        potentiallyAmbiguousArguments.Add((argIndex, argument, isPrimaryArgument));
                        break;
                    }
                    case ArgumentMarshallingMode.DelegateManaged or
                        ArgumentMarshallingMode.DelegateFunctionPointer or
                        ArgumentMarshallingMode.DelegateIntPtr: {
                        bool isPrime = marshallingMode == ArgumentMarshallingMode.DelegateIntPtr;
                        potentiallyAmbiguousArguments.Add((argIndex, argument, isPrime));
                        break;
                    }
                }
            }
        }

        // Only keep overloaded ambiguous arguments
        potentiallyAmbiguousArguments =
            potentiallyAmbiguousArguments
                .GroupBy(x => x.argumentIndex)
                .Where(x => x.Count() > 1)
                .SelectMany(x => x)
                .ToList();

        int? firstArgumentAmbiguousInAnySignature = null;
        int? lastArgumentAmbiguousInAnySignature = null;

        foreach ((int argumentIndex, _, _) in potentiallyAmbiguousArguments) {
            if (firstArgumentAmbiguousInAnySignature == null || argumentIndex < firstArgumentAmbiguousInAnySignature) {
                firstArgumentAmbiguousInAnySignature = argumentIndex;
            }

            if (lastArgumentAmbiguousInAnySignature == null || argumentIndex > lastArgumentAmbiguousInAnySignature) {
                lastArgumentAmbiguousInAnySignature = argumentIndex;
            }
        }

        if (firstArgumentAmbiguousInAnySignature == null || lastArgumentAmbiguousInAnySignature == null)
            return;

        // Disable default values for ambiguous arguments
        for (int i = firstArgumentAmbiguousInAnySignature.Value; i <= lastArgumentAmbiguousInAnySignature.Value; i++) {
            foreach ((int argumentIndex, ManagedMethodArgument argument, bool isPrime) in potentiallyAmbiguousArguments) {
                if (argumentIndex == i && !isPrime) {
                    argument.MarshalledArgument.IsDefaultValueValidInDeclaration = false;
                }
            }
        }
    }

    private List<MethodArgumentOverloadRaw> FilterRawMethodArgumentOverloads(List<MethodArgumentOverloadRaw> overloads) {
        List<MethodArgumentOverloadRaw> filteredOverloads = [];
        foreach (MethodArgumentOverloadRaw overload in overloads) {
            if (!CheckStringBeginEndPair(overload))
                continue;

            if (!CheckUtf8StringWriter(overload))
                continue;

            filteredOverloads.Add(overload);
        }

        return filteredOverloads;

        bool CheckUtf8StringWriter(MethodArgumentOverloadRaw overload) {
            return true;
            int utf8StringWriterSignatureArgumentsCount =
                overload.Arguments
                    .Count(arg => arg.ArgumentMarshallingMode == ArgumentMarshallingMode.StringImGuiUtf8StringWriter);

            if (utf8StringWriterSignatureArgumentsCount == 0)
                return true;

            // Only keep a single overload per position,
            // because Utf8StringWriter can't handle more than one Utf8StringWriter argument per method
            // because it uses a shared buffer instance
            if (utf8StringWriterSignatureArgumentsCount > 1)
                return false;

            (int Index, NativeFunctionArgumentMarshallingVariant Item) firstUtf8StringWriterArgumentAcrossAllOverloads =
                overloads
                    .Select(o =>
                        o.Arguments
                            .Index()
                            .FirstOrDefault(
                                argWithIndex =>
                                    argWithIndex.Item.ArgumentMarshallingMode == ArgumentMarshallingMode.StringImGuiUtf8StringWriter
                            )
                    )
                    .MinBy(argWithIndex => argWithIndex.Index);

            return
                overload.Arguments[firstUtf8StringWriterArgumentAcrossAllOverloads.Index].ArgumentMarshallingMode ==
                ArgumentMarshallingMode.StringImGuiUtf8StringWriter;
        }

        bool CheckStringBeginEndPair(MethodArgumentOverloadRaw overload) {
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
                    return false;
            }

            return true;
        }
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
        static string FormatInvocationArgument(FunctionArgument argument) {
            string result = argument.Name;
            result = argument.PassType switch {
                FunctionArgumentPassType.Normal => "",
                FunctionArgumentPassType.Ref => "ref ",
                FunctionArgumentPassType.Out => "out ",
                FunctionArgumentPassType.In => "in ",
                _ => throw new ArgumentOutOfRangeException()
            } + result;

            return result;
        }

        string invocationValuesFormatted =
            String.Join(", ", managedMethod.Arguments.Select(FormatInvocationArgument));

        string invocation =
            managedMethod.IsConstructor ?
                $"{nativeFunctionsClassName}.{nativeFunction.Name}({invocationValuesFormatted})" :
                $"{managedMethod.Name}({invocationValuesFormatted})";

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
        ArgumentMarshallerOutput[] argumentMarshallerOutputs =
            methodSignature.Arguments
                .Select(arg => ArgumentMarshallerFactory.Create(methodSignature, arg).Marshall())
                .ToArray();

        string invocationValuesFormatted =
            String.Join(", ", argumentMarshallerOutputs.Select(x => x.InvocationExpression));
        string nativeFunctionInvocation =
            $"{nativeFunctionsClassName}.{methodSignature.NativeFunction.Name}({invocationValuesFormatted})";

        IReturnTypeMarshaller returnTypeMarshaller =
            ReturnTypeMarshallerFactory.Create(methodSignature, nativeFunctionInvocation);

        StatementSyntaxListBuilder bodyBuilder = new();

        foreach (ArgumentMarshallerOutput argumentMarshallerOutput in argumentMarshallerOutputs) {
            bodyBuilder.AppendRange(argumentMarshallerOutput.MarshallStatements);
        }

        bodyBuilder.AppendRange(returnTypeMarshaller.CreateInvokeStatements());

        foreach (ArgumentMarshallerOutput argumentMarshallerOutput in argumentMarshallerOutputs) {
            bodyBuilder.AppendRange(argumentMarshallerOutput.UnmarshallStatements);
        }

        bodyBuilder.AppendRange(returnTypeMarshaller.CreateReturnStatements());

        List<FunctionArgument> signatureArguments =
            methodSignature.Arguments
                .Where(x => x.IsPartOfDeclaration)
                .Select(x => x.MarshalledArgument)
                .ToList();

        string? methodNameSuffix = methodSignature.ReturnType.MarshallingVariant.ManagedMethodNameSuffix;
        if (methodNameSuffix != null) {
            methodName += methodNameSuffix;
        }

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

        // Only enable Utf8StringWriter if a conditional flag is set
        bool anyUtf8StringWriterSignatureArguments =
            signatureArguments
                .Any(arg =>
                    arg.Type.ExpandTypedef().Value == KnownEntities.ImGuiUtf8StringWriter
                );

        if (anyUtf8StringWriterSignatureArguments) {
            managedMethod.Conditionals = [
                ..managedMethod.Conditionals,
                new Conditional("USE_UTF8_STRING_WRITER")
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

        // Base overload with all possible arguments
        ManagedMethodArgument[] baseArguments = baseMethod.Arguments.ToArray();
        DisableDefaultValuesInArguments(baseArguments, lastInvalidArgument);

        MethodSignature baseOverload = baseMethod with { Arguments = baseArguments };
        result.Add(baseOverload);

        for (int argIndex = lastInvalidArgument; argIndex >= firstInvalidArgument; argIndex--) {
            ManagedMethodArgument[] overloadArguments = baseMethod.Arguments.ToArray();

            if (argIndex == firstInvalidArgument) {
                // We've ran out of invalid arguments, no need to disable default values anymore
                RemoveInvalidArgumentsFromDeclaration(overloadArguments, argIndex);
            } else {
                DisableDefaultValuesInArguments(overloadArguments, lastInvalidArgument);
                RemoveInvalidArgumentsFromDeclaration(overloadArguments, argIndex);
            }

            MethodSignature overload = baseMethod with { Arguments = overloadArguments };
            result.Add(overload);
        }

        Debug.Assert(result.Count != 0);
        return result;

        static void DisableDefaultValuesInArguments(
            IList<ManagedMethodArgument> overloadArguments,
            int lastInvalidArgument
        ) {
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
        if (description.IsVoid)
            return [new(description.ReturnType, ReturnTypeMarshallingMode.Void, null)];

        if (description.IsString)
            return [
                new(
                    description.ReturnType,
                    ReturnTypeMarshallingMode.NullTerminatedString,
                    null
                )
            ];

        NativeFunctionReturnTypeMarshallingVariant passthroughVariant = new(
            description.ReturnType,
            ReturnTypeMarshallingMode.Passthrough,
            null
        );

        NativeFunctionReturnTypeMarshallingVariant pointerWrapperVariant = new(
            description.ReturnType,
            ReturnTypeMarshallingMode.PointerWrapper,
            null
        );

        if (description is { IsPointer: true }) {
            if (description.IsKnownNonNull) {
                NativeFunctionReturnTypeMarshallingVariant refVariant = new(
                    description.ReturnType,
                    ReturnTypeMarshallingMode.Ref,
                    null
                );

                // FIXME: this shouldn't be done here
                pointerWrapperVariant = pointerWrapperVariant with {
                    ManagedMethodNameSuffix = "Pointer"
                };

                return [refVariant, pointerWrapperVariant];
            } else {
                return [pointerWrapperVariant];
            }
        }

        return [passthroughVariant];
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
                AddVariant(ArgumentMarshallingMode.StringBytePointer, StringBeginEndPairMode.Begin);

                AddVariant(ArgumentMarshallingMode.StringReadOnlyByteSpan, StringBeginEndPairMode.BeginOnly);
                AddVariant(ArgumentMarshallingMode.StringReadOnlyCharSpan, StringBeginEndPairMode.BeginOnly);
                AddVariant(ArgumentMarshallingMode.StringImGuiUtf8StringWriter, StringBeginEndPairMode.BeginOnly);
                AddVariant(ArgumentMarshallingMode.StringManaged, StringBeginEndPairMode.BeginOnly);
            } else if (isStringBeginEndPairEnd) {
                AddVariant(ArgumentMarshallingMode.StringBytePointer, StringBeginEndPairMode.End);
                AddVariant(ArgumentMarshallingMode.StringBytePointer, StringBeginEndPairMode.EndDefault);
            } else {
                AddVariant(ArgumentMarshallingMode.StringBytePointer);
                AddVariant(ArgumentMarshallingMode.StringReadOnlyByteSpan);
                AddVariant(ArgumentMarshallingMode.StringReadOnlyCharSpan);
                AddVariant(ArgumentMarshallingMode.StringImGuiUtf8StringWriter);
                AddVariant(ArgumentMarshallingMode.StringManaged);
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
            case ReturnTypeMarshallingMode.PointerWrapper: {
                Pointer? returnTypePointer = returnType.AsEntityReference().ExpandTypedef().Value as Pointer;
                Debug.Assert(returnTypePointer != null);

                // Uglyness to handle void**
                if (returnTypePointer.ValueType.Value.IsVoidPointer()) {
                    returnTypePointer = returnTypePointer with {
                        ValueType = KnownEntities.IntPtr.AsEntityReference()
                    };
                }

                returnType = new Builtin($"Pointer<{CSharpCodeGenerationUtility.FormatType(returnTypePointer.ValueType.Value)}>", null, StorageClass.NotSpecified);
                break;
            }
            case ReturnTypeMarshallingMode.Ref: {
                Pointer? returnTypePointer = returnType.AsEntityReference().ExpandTypedef().Value as Pointer;
                Debug.Assert(returnTypePointer != null);

                // Uglyness to handle void**
                if (returnTypePointer.ValueType.Value.IsVoidPointer()) {
                    returnTypePointer = returnTypePointer with {
                        ValueType = KnownEntities.IntPtr.AsEntityReference()
                    };
                }

                returnType = returnTypePointer with {
                    PointerType = BindingsModel.PointerType.ManagedRef
                };
                break;
            }
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
        for (int index = 0; index < nativeArgumentMarshallingVariants.Count; index++) {
            NativeFunctionArgumentMarshallingVariant nativeArgumentMarshallingVariant = nativeArgumentMarshallingVariants[index];
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
                case ArgumentMarshallingMode.StringReadOnlyByteSpan:
                    managedArgument.Type = KnownEntities.ReadOnlySpanByte.AsEntityReference();
                    break;
                case ArgumentMarshallingMode.StringReadOnlyCharSpan:
                    managedArgument.Type = KnownEntities.ReadOnlySpanChar.AsEntityReference();
                    break;
                case ArgumentMarshallingMode.StringImGuiUtf8StringWriter:
                    managedArgument.PassType = FunctionArgumentPassType.In;
                    managedArgument.Type = KnownEntities.ImGuiUtf8StringWriter.AsEntityReference();
                    break;
                case ArgumentMarshallingMode.StringManaged:
                    managedArgument.Type = KnownEntities.String.AsEntityReference();
                    break;
                case ArgumentMarshallingMode.StringBytePointer:
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
                    index,
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
        public bool IsPointer { get; init; }
        public bool IsKnownNonNull { get; init; }
    }
}

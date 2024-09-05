using System.Diagnostics;
using CaseConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using NovaImGuiNet.Generator.Metadata;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CSharpCodeGenerationUtility;
using static SyntaxFactory;

public static class ManagedMethodGenerator {
    private static readonly ILogger Log = Logging.Create(nameof(ManagedMethodGenerator));

    public static BindingsModel Generate(BindingsModel model) {
        IGrouping<EntityReferenceWrapper<Struct>?, EntityReferenceWrapper<Function>>[] allFunctionsByStruct =
            model.GetAllFunctions()
                .GroupBy(x => x.Value.OwnerStruct)
                .ToArray();

        foreach (var functionsByStruct in allFunctionsByStruct) {
            EntityReferenceWrapper<Struct>? ownerStruct = functionsByStruct.Key;
            EntityReferenceWrapper<Function>[] structFunctions = functionsByStruct.ToArray();
            if (ownerStruct == null) {
                IGrouping<string?, EntityReferenceWrapper<Function>>[] allFreeFunctionsByNamespaces =
                    structFunctions.GroupBy(x => x.Value.Namespace).ToArray();

                foreach (var freeFunctionsByNamespace in allFreeFunctionsByNamespaces) {
                    Struct imGuiMethodsStruct = CreateImGuiManagedMethodsStruct(freeFunctionsByNamespace.Key);
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

    private static Struct CreateImGuiManagedMethodsStruct(string ns) {
        Struct structure = new() {
            Kind = Struct.StructKind.StaticClass,
            Name = "ImGui",
            Namespace = ns,
            Comments = new Comments(["Dear ImGui functions"], [], [])
        };

        return structure;
    }

    private static void GenerateManagedMethodsForStruct(
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

    private static List<Function> GenerateManagedMethodsForNativeFunction(
        Struct structure,
        Function nativeFunction,
        string nativeFunctionBaseName
    ) {
        if (nativeFunction.IsDisabled)
            return [];

        bool isInstanceMethod = nativeFunction.OwnerStruct != null;

        string methodName = nativeFunctionBaseName;
        if (isInstanceMethod) {
            methodName = methodName
                .RemoveFromStart(structure.Name + "_")
                .FirstCharToUpperCase();
        } else {
            bool isFreeFunction = nativeFunctionBaseName.StartsWith("cIm", StringComparison.Ordinal);
            if (!isFreeFunction) {
                methodName = methodName
                    .RemoveFromStart("ImGui_")
                    .FirstCharToUpperCase();
            }
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

        const bool isFunctionBanned = false;

        NativeFunctionArgumentMarshalingVariant[][] nativeArgumentMarshalingVariants =
            nativeArgumentDescriptions
                .Select(arg => ResolveNativeArgumentMarshalingVariants(arg, isFunctionBanned))
                .ToArray();

        List<MethodOverloadRaw> rawMethodOverloads = [];
        GenerateArgumentMarshalingVariantsMethodOverloads(nativeArgumentMarshalingVariants, rawMethodOverloads, null, 0, 0);

        List<MethodSignature> methodSignatures =
            rawMethodOverloads
                .Select(raw => new MethodSignature(nativeFunction, ResolveManagedMethodArguments(raw.Arguments).ToEqualityList()))
                .ToList();

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
                    .DistinctBy(x => x.CreateDiscriminator())
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

        List<Function> managedMethods = [];
        foreach (MethodSignature methodSignature in methodSignatures) {
            managedMethods.Add(GenerateManagedMethodForNativeFunction(structure, nativeFunction, methodName, methodSignature));
        }

        return managedMethods;
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
        Function nativeFunction,
        string methodName,
        MethodSignature methodSignature
    ) {
        bool isInstanceMethod = nativeFunction.OwnerStruct != null;

        string thisPointerVar = InternalVariableName("thisPtr");
        string retValVar = InternalVariableName("retVal");

        List<ManagedMethodArgument> nativeInvocationArguments =
            methodSignature.Arguments
                .Skip(isInstanceMethod ? 1 : 0)
                .ToList();

        List<string> nativeInvocationValues =
            nativeInvocationArguments
                .Select(arg => InternalVariableName(arg.MarshaledArgument.Name))
                .ToList();

        if (isInstanceMethod) {
            nativeInvocationValues.Insert(0, thisPointerVar);
        }

        bool hasReturnValue =
            nativeFunction.ReturnType.ExpandTypedef()?.Value is not Builtin builtin ||
            builtin.ManagedType != typeof(void);

        List<StatementSyntax> statements = [];

        void Append(string text) {
            statements.Add(ParseStatement(text).AddLeadingTriviaToEnd(LineFeed));
        }

        Append("throw new Exception();");

        /*FormatTypeOptions varTypeFormatOptions = new() {
            ExpandTypedefs = false,
        };

        if (isInstanceMethod) {
            Append($"{structure.Name}* {thisPointerVar} = ({structure.Name}*) Unsafe.AsPointer(ref this);");
        }

        foreach (FunctionArgument arg in invocationArguments) {
            Append($"{FormatType(arg.Type.Value, varTypeFormatOptions)} {InternalVariableName(arg.Name)} = {arg.Name};");
        }

        if (hasReturnValue) {
            Append($"{FormatType(nativeFunction.ReturnType.Value, varTypeFormatOptions)} {retValVar};");
        }

        string retValAssignment = hasReturnValue ? $"{retValVar} = " : "";
        string invocationValuesFormatted = String.Join(", ", invocationValues);
        Append($"{retValAssignment}ImGuiNative.{nativeFunction.Name}({invocationValuesFormatted});");

        if (hasReturnValue) {
            Append($"return {retValVar};");
        }*/

        List<FunctionArgument> signatureArguments =
            nativeInvocationArguments
                .Where(x => x.IsPartOfDeclaration)
                .Select(x => x.MarshaledArgument)
                .ToList();

        Function copy = nativeFunction with {
            Name = methodName,
            Body = Block(statements),
            IsNativeFunction = false,
            Arguments = signatureArguments
        };

        return copy;

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
            bool isVoidPointer = description.Argument.Type.Value is Pointer { ValueType.Value: Builtin { BuiltinName: "void" } };
            bool isVoidPointerPointer = description.Argument.Type.Value is Pointer { ValueType.Value: Pointer { ValueType.Value: Builtin { BuiltinName: "void" } } };

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
                    managedArgument.Type = managedArgument.Type.ExpandTypedefsAndPointers()!;
                    managedArgument.IsDefaultValueValidInDeclaration = false;

                    break;
                case PointerMarshalMode.Out:
                    managedArgument.PassType = FunctionArgumentPassType.Out;
                    managedArgument.Type = managedArgument.Type.ExpandTypedefsAndPointers();
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
                    true
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

    public record MethodOverloadRaw(IReadOnlyList<NativeFunctionArgumentMarshalingVariant> Arguments);

    public record MethodSignature(Function NativeFunction, IReadOnlyList<ManagedMethodArgument> Arguments) {
        public override string ToString() {
            List<string> argumentDescriptions = [];
            for (int argumentIndex = 0; argumentIndex < Arguments.Count; argumentIndex++) {
                ManagedMethodArgument argument = Arguments[argumentIndex];

                string argumentDescription = $"[{argumentIndex + 1}] ";

                if (!argument.IsPartOfDeclaration) {
                    argumentDescription += " (def-only)";
                } else {
                    argumentDescription += $" ({argument.MarshalingVariant})";
                }

                argumentDescriptions.Add(argumentDescription);
            }

            return $"{NativeFunction.Name} ({String.Join(", ", argumentDescriptions)}";
        }

        public MethodSignatureArgumentDiscriminator CreateDiscriminator() {
            EqualityList<ArgumentDiscriminator> list = [];
            foreach (ManagedMethodArgument argument in Arguments) {
                if (argument.IsPartOfDeclaration) {
                    list.Add(
                        new ArgumentDiscriminator(
                            true,
                            argument.MarshalingVariant.StringMarshalingMode,
                            argument.MarshalingVariant.PointerMarshalMode
                        )
                    );
                } else {
                    list.Add(new ArgumentDiscriminator(false, StringMarshalingMode.Undefined, PointerMarshalMode.Undefined));
                    list.Add(new ArgumentDiscriminator(false, StringMarshalingMode.Undefined, PointerMarshalMode.Undefined));
                }
            }

            return new MethodSignatureArgumentDiscriminator(list);
        }

        public record MethodSignatureArgumentDiscriminator(EqualityList<ArgumentDiscriminator> Arguments);

        public record ArgumentDiscriminator(
            bool IsPartOfDeclaration,
            StringMarshalingMode StringMarshalingMode,
            PointerMarshalMode PointerMarshalMode
        );
    }

    public readonly record struct ManagedMethodArgument(
        FunctionArgument NativeArgument,
        FunctionArgument MarshaledArgument,
        NativeFunctionArgumentMarshalingVariant MarshalingVariant,
        bool IsPartOfDeclaration
    );

    public readonly record struct NativeFunctionArgumentMarshalingVariant(
        FunctionArgument NativeFunctionArgument,
        int NativeFunctionArgumentIndex,
        StringMarshalingMode StringMarshalingMode,
        PointerMarshalMode PointerMarshalMode
    ) {
        public override string ToString() {
            List<string> elements = [];

            if (StringMarshalingMode != StringMarshalingMode.Undefined) {
                elements.Add($"String = {StringMarshalingMode}");
            }

            if (PointerMarshalMode != PointerMarshalMode.Undefined) {
                elements.Add($"Pointer = {PointerMarshalMode}");
            }

            return $"{NativeFunctionArgument.Name} ({String.Join(", ", elements)})";
        }
    }

    public readonly record struct NativeFunctionArgumentDescription(FunctionArgument Argument, int ArgumentIndex) {
        public bool IsString { get; init; }
        public bool IsPointer { get; init; }
    }

    public enum StringMarshalingMode {
        Undefined,

        BytePointer,
        ReadOnlyByteSpan,
        ReadOnlyCharSpan,
        String,
    }

    public enum PointerMarshalMode {
        Undefined,

        Pointer,
        Ref,
        Out,

        IntPtr,
        IntPtrRef,
        IntPtrOut,
    }
}

using System.Diagnostics;
using CaseConverter;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CSharpCodeGenerationUtility;
using static SyntaxFactory;

public static class StructFunctionGenerator {
    private static readonly ILogger Log = Logging.Create(nameof(StructFunctionGenerator));

    public static BindingsModel Generate(BindingsContext context, BindingsModel model) {
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
                    Struct imGuiMethodsStruct = GenerateImGuiMethodsStruct(freeFunctionsByNamespace.Key);
                    IEntityReference imGuiMethodsStructReference = imGuiMethodsStruct.AsEntityReference();
                    model.Children.Add(imGuiMethodsStructReference);

                    ownerStruct = new EntityReferenceWrapper<Struct>(imGuiMethodsStructReference);
                    GenerateFor(ownerStruct, freeFunctionsByNamespace.ToArray());
                }
            } else {
                GenerateFor(ownerStruct, structFunctions);
            }
        }

        return model;
    }

    private static Struct GenerateImGuiMethodsStruct(string ns) {
        Struct structure = new() {
            Kind = Struct.StructKind.StaticClass,
            Name = "ImGui",
            Namespace = ns,
            Comments = new Comments(["Dear ImGui functions"], [], [])
        };

        return structure;
    }

    private static void GenerateFor(
        EntityReferenceWrapper<Struct> structureReference,
        EntityReferenceWrapper<Function>[] functionReferences
    ) {
        Struct structure = structureReference.Value;

        Entity[] structFunctionsNew =
            functionReferences
                .SelectMany(function => GenerateFor(structure, function.Value))
                .ToArray();
        structure.Children.AddRange(structFunctionsNew);
    }

    private static IReadOnlyList<Entity> GenerateFor(Struct structure, Function nativeFunction) {
        if (nativeFunction.IsDisabled)
            return [];

        bool isInstanceMethod = nativeFunction.OwnerStruct != null;

        string prettyName = nativeFunction.Name;
        if (isInstanceMethod) {
            prettyName = prettyName
                .RemoveFromStart(structure.Name + "_")
                .FirstCharToUpperCase();
        } else {
            bool isFreeFunction = nativeFunction.Name.StartsWith("cIm", StringComparison.Ordinal);
            if (!isFreeFunction) {
                prettyName = prettyName
                    .RemoveFromStart("ImGui_")
                    .FirstCharToUpperCase();
            }
        }

        string thisPointerVar = InternalVariableName("thisPtr");
        string retValVar = InternalVariableName("retVal");

        List<FunctionArgument> invocationArguments =
            nativeFunction.Arguments
                .Skip(isInstanceMethod ? 1 : 0)
                .ToList();

        List<string> invocationValues =
            invocationArguments
                .Select(arg => InternalVariableName(arg.Name))
                .ToList();

        if (isInstanceMethod) {
            invocationValues.Insert(0, thisPointerVar);
        }

        bool hasReturnValue =
            nativeFunction.ReturnType.ExpandTypedef()?.Value is not Builtin builtin ||
            builtin.ManagedType != typeof(void);

        List<StatementSyntax> statements = [];

        void Append(string text) {
            statements.Add(ParseStatement(text).AddLeadingTriviaToEnd(LineFeed));
        }

        FormatTypeOptions varTypeFormatOptions = new() {
            DontExpandTypedefs = true,
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
        }

        Function copy = nativeFunction with {
            Name = prettyName,
            Body = Block(statements),
            IsNativeFunction = false,
            Arguments = invocationArguments
        };

        return [copy];

        static string InternalVariableName(string name) {
            return "__" + name;
        }
    }
}

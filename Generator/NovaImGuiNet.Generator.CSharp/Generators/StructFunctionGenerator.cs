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
        IGrouping<EntityReferenceWrapper<Struct>?, EntityReferenceWrapper<Function>>[] allFunctions =
            model.GetAllFunctions()
                .GroupBy(x => x.Value.OwnerStruct)
                .ToArray();

        foreach (var functionsByStruct in allFunctions) {
            EntityReferenceWrapper<Struct>? ownerStruct = functionsByStruct.Key;
            EntityReferenceWrapper<Function>[] structFunctions = functionsByStruct.ToArray();
            if (ownerStruct == null) {
                Struct imGuiMethodsStruct = GenerateImGuiMethodsStruct(context);
                IEntityReference imGuiMethodsStructReference = imGuiMethodsStruct.AsEntityReference();
                model.Children.Add(imGuiMethodsStructReference);

                ownerStruct = new EntityReferenceWrapper<Struct>(imGuiMethodsStructReference);
            }

            GenerateFor(ownerStruct, structFunctions);
        }

        return model;
    }

    private static Struct GenerateImGuiMethodsStruct(BindingsContext context) {
        Struct structure = new() {
            Kind = Struct.StructKind.StaticClass,
            Name = "ImGui",
            Namespace = context.Namespace,
            Comments = new Comments(["Dear ImGui functions"], [], [])
        };

        return structure;
    }

    private static void GenerateFor(
        EntityReferenceWrapper<Struct> structureReference,
        EntityReferenceWrapper<Function>[] functionReferences
    ) {
        Struct structure = structureReference.Value;

        IEntityReference[] structFunctionsNew =
            functionReferences
                .SelectMany(function => GenerateFor(structure, function.Value))
                .Select(x => x.AsEntityReference())
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
                .ToPascalCase();
        } else {
            if (!prettyName.StartsWith("cIm", StringComparison.Ordinal)) {
                prettyName = prettyName
                    .RemoveFromStart("ImGui_")
                    .ToPascalCase();
            }
        }

        List<FunctionArgument> invocationArguments =
            nativeFunction.Arguments
                .Skip(isInstanceMethod ? 1 : 0)
                .ToList();

        List<string> invocationValues =
            invocationArguments
                .Select(arg => arg.Name)
                .ToList();

        if (isInstanceMethod) {
            invocationValues.Insert(0, "__thisPtr");
        }

        string invocationValuesFormatted = String.Join(", ", invocationValues);

        bool hasReturnValue =
            nativeFunction.ReturnType.ExpandTypedef()?.Value is not Builtin builtin ||
            builtin.ManagedType != typeof(void);

        string retValAssignment = hasReturnValue ? "__retVal = " : "";

        List<StatementSyntax> statements = [];

        void Append(string text) {
            statements.Add(ParseStatement(text).AddLeadingTriviaToEnd(LineFeed));
        }

        if (isInstanceMethod) {
            Append($"{structure.Name}* __thisPtr = ({structure.Name}*) Unsafe.AsPointer(ref this);");
        }

        if (hasReturnValue) {
            Append($"{FormatType(nativeFunction.ReturnType.Value)} __retVal;");
        }

        Append($"{retValAssignment}ImGuiNative.{nativeFunction.Name}({invocationValuesFormatted});");

        if (hasReturnValue) {
            Append("return __retVal;");
        }

        Function copy = nativeFunction with {
            Name = prettyName,
            Body = Block(statements),
            IsNativeFunction = false,
            Arguments = invocationArguments
        };

        return [copy];
    }
}

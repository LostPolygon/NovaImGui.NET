using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static SyntaxFactory;

public static class CSharpCodeGenerationUtility {
    private static readonly IReadOnlyList<string> AlwaysUseAliasesForBuiltins = ["void"];

    public static string FormatType(IEntity type, in FormatTypeOptions options = default) {
        Debug.Assert(type != null, "type != null");

        switch (type) {
            case Builtin builtin:
                if (options.FullyQualifiedBuiltins && !AlwaysUseAliasesForBuiltins.Contains(builtin.BuiltinName)) {
                    return builtin.ManagedType?.FullName ?? builtin.BuiltinName;
                }

                return builtin.BuiltinName;
            case Array array:
                return $"{FormatType(array.ElementType.Value, options)}[{array.ArrayBounds}]";
            case Pointer pointer:
                string valueTypeFormatted = FormatType(pointer.ValueType.Value, options);
                return options.UsePointerWrapper ? $"Pointer<{valueTypeFormatted}>" : $"{valueTypeFormatted}*";
            case Enum userEnum:
                return $"{userEnum.Name}";
            case Function userFunction:
                return FormatFunction(userFunction);
            case Struct userStruct: {
                string result = userStruct.Name;
                if (userStruct.GenericArguments.Count != 0) {
                    IEnumerable<string> genericArgumentsFormatted =
                        userStruct.GenericArguments
                            .Select(x =>
                                FormatType(x.Value, new FormatTypeOptions { UsePointerWrapper = true })
                            );
                    result += $"<{String.Join(", ", genericArgumentsFormatted)}>";
                }

                if (options.FullyQualifiedTypes) {
                    result = $"{userStruct.Namespace}.{result}";
                }

                return result;
            }
            case Typedef userTypedef:
                bool expand = !options.DontExpandTypedefs;
                if (!expand &&
                    options.DelegatesAsFunctionPointers &&
                    userTypedef.Type.Value is Delegate
                   ) {
                    expand = true;
                }

                if (expand) {
                    return FormatType(userTypedef.Type.Value, options);
                }

                return userTypedef.Name;
            case Delegate userDelegate:
                return
                    options.DelegatesAsFunctionPointers ?
                        FormatUnmanagedFunctionPointer(userDelegate) :
                        $"{userDelegate.Name}";
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public static string FormatUnmanagedFunctionPointer(Delegate userDelegate) {
        string functionPointer = "delegate* unmanaged<";
        List<string> functionPointerGenericArguments =
            userDelegate.Arguments
                .Select(a => FormatType(a.Type.Value))
                .ToList();
        functionPointerGenericArguments.Add(FormatType(userDelegate.ReturnType.Value));
        functionPointer += String.Join(", ", functionPointerGenericArguments);
        functionPointer += ">";
        return functionPointer;
    }

    public static string FixInvalidIdentifier(string identifier) {
        bool isFirstCharacterIllegal = Char.IsNumber(identifier[0]);
        if (isFirstCharacterIllegal) {
            identifier = $"_{identifier}";
        }

        bool isReservedKeyword = SyntaxFacts.IsReservedKeyword(ParseToken(identifier).Kind());
        if (isReservedKeyword) {
            identifier = $"{identifier}_";
        }

        return identifier;
    }

    public static string? GetBuiltinTypeAlias(Type type) {
        if (type.IsEnum)
            return null;

        return Type.GetTypeCode(type) switch {
            TypeCode.Object => type.BaseType == null ? "object" : null,
            TypeCode.Boolean => "bool",
            TypeCode.Char => "char",
            TypeCode.SByte => "sbyte",
            TypeCode.Byte => "byte",
            TypeCode.Int16 => "short",
            TypeCode.UInt16 => "ushort",
            TypeCode.Int32 => "int",
            TypeCode.UInt32 => "uint",
            TypeCode.Int64 => "long",
            TypeCode.UInt64 => "ulong",
            TypeCode.Single => "float",
            TypeCode.Double => "double",
            TypeCode.Decimal => "decimal",
            TypeCode.String => "string",
            _ => null
        };
    }

    private static string FormatFunction(Function function) {
        string functionPointer = $"{FormatType(function.ReturnType.Value)} {function.Name}(";
        List<string> functionPointerGenericArguments =
            function.Arguments
                .Select(arg => $"{FormatType(arg.Type.Value)} {arg.Name}")
                .ToList();
        functionPointer += String.Join(", ", functionPointerGenericArguments);
        functionPointer += ")";
        return functionPointer;
    }

    public readonly record struct FormatTypeOptions {
        public bool FullyQualifiedBuiltins { get; init; }
        public bool FullyQualifiedTypes { get; init; }
        public bool DelegatesAsFunctionPointers { get; init; }
        public bool UsePointerWrapper { get; init; }
        public bool DontExpandTypedefs { get; init; }
    }
}

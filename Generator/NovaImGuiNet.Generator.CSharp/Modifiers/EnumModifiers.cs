using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace NovaImGuiNet.Generator.CSharp;

using static BindingsModel;
using static CSharpCodeGenerationUtility;

public static class EnumModifiers {
    private static readonly ILogger Log = Logging.Create(nameof(EnumModifiers));

    public static Enum PatchEnumsReferencingOtherEnums(Enum enumeration) {
        switch (enumeration.Name) {
            case "ImDrawCornerFlags":
                return enumeration with {
                    Members = enumeration.Members
                        .Select(member => member with { Value = member.Value!.Replace("ImDrawFlags_", "ImDrawFlags.") })
                        .ToList()
                };
            case "ImGuiKey":
                return enumeration with {
                    Members = enumeration.Members
                        .Where(member => member.Value == null || !member.Value!.StartsWith("ImGuiMod_", StringComparison.Ordinal))
                        .Select(member => member with { Name = member.Name.Replace("ImGuiMod_", "Mod") })
                        .ToList()
                };
            case "ImGuiModFlags":
                return enumeration with {
                    Members = enumeration.Members
                        .Select(member => member with { Value = member.Value!.Replace("ImGuiMod_", "ImGuiKey.Mod") })
                        .ToList()
                };
        }

        return enumeration;
    }

    public static Enum AddPublicMembersToPrivateEnum(BindingsModel model, Enum enumeration) {
        string cleanedName = CleanupEnumIdentifier(enumeration.Name, null);
        if (!cleanedName.EndsWith("Private", StringComparison.Ordinal))
            return enumeration;

        string publicEnumNameGuess = cleanedName.RemoveSuffix("Private");
        Enum? publicEnum = model.Enums
            .FirstOrDefault(x => CleanupEnumIdentifier(x.Value.Name, null) == publicEnumNameGuess)
            ?.Value;

        if (publicEnum == null) {
            Log.LogWarning($"No public enum found for private enum {enumeration.Name}");
            return enumeration;
        }

        enumeration.Members.InsertRange(0, publicEnum.Members);
        return enumeration;
    }

    public static Enum CleanupIdentifiers(Enum enumeration) {
        string initialEnumName = enumeration.Name;
        return enumeration with {
            Name = CleanupEnumIdentifier(initialEnumName, null),
            Members =
            enumeration.Members
                .Select(member => member with {
                    Name = FixInvalidIdentifier(CleanupEnumIdentifier(member.Name, initialEnumName)),
                    Value = member.Value != null ? CleanupEnumIdentifier(member.Value, initialEnumName, true) : null
                })
                .ToList()
        };
    }

    public static string CleanupEnumIdentifier(string text, string? typeName, bool isValue = false) {
        if (typeName != null) {
            typeName = RemoveTrailingUnderscoresFromExpression(typeName);
            if (typeName.EndsWith("Private")) {
                string nonPrivateTypeName = typeName.RemoveSuffix("Private");
                if (false && isValue) {
                    //text = RemoveTypeName(text, typeName);
                    //text = text.Replace(nonPrivateTypeName + "_", nonPrivateTypeName + ".");
                }

                text = RemoveTypeName(text, nonPrivateTypeName);
            }

            text = RemoveTypeName(text, typeName);
        }

        text = RemoveTrailingUnderscoresFromExpression(text);
        return text;
    }

    private static string RemoveTypeName(string text, string typeName) =>
        text.Replace(typeName + "_", "")
            .Replace(typeName, "");

    // Removed trailing underscores from all words in the text
    private static string RemoveTrailingUnderscoresFromExpression(string expression) =>
        Regex.Replace(
            expression,
            @"(\w+)_($|\s)", // Match any word characters ending with an underscore followed by whitespace or end of string
            "$1$2",
            RegexOptions.Singleline | RegexOptions.CultureInvariant | RegexOptions.Compiled
        );
}

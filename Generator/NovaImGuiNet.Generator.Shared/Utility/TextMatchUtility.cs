using System.Text.RegularExpressions;

namespace NovaImGuiNet.Generator;

public static class TextMatchUtility {
    /// <param name="text"></param>
    /// <param name="patterns">
    /// If a pattern starts and ends with a / (slash), it will be treated as a regex.
    /// Otherwise, it needs to be an exact match for <paramref name="text"/>.
    /// </param>
    /// <param name="stringPatternComparisonType"></param>
    public static bool IsMatchAny(
        string text,
        IReadOnlyList<string> patterns,
        StringComparison stringPatternComparisonType = StringComparison.Ordinal
    ) {
        foreach (string pattern in patterns) {
            int firstSlash = pattern.IndexOf('/');
            int lastSlash = pattern.LastIndexOf('/');
            bool isRegex = firstSlash == 0 && lastSlash == pattern.Length - 1;

            if (isRegex) {
                string regex = pattern.Substring(firstSlash + 1, lastSlash - 1);
                if (Regex.IsMatch(text, regex))
                    return true;
            } else {
                if (text.Equals(pattern, stringPatternComparisonType))
                    return true;
            }
        }

        return false;
    }
}

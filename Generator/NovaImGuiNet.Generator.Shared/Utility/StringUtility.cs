namespace NovaImGuiNet.Generator;

public static class StringUtility {
    public static string RemovePrefix(this string str, string prefix, StringComparison comparison = StringComparison.Ordinal) {
        int index = str.IndexOf(prefix, comparison);
        if (index != 0)
            return str;

        str = str.Remove(0, prefix.Length);
        return str;
    }

    public static string RemoveSuffix(this string str, string suffix, StringComparison comparison = StringComparison.Ordinal) {
        int index = str.LastIndexOf(suffix, comparison);
        if (index == -1 || index != str.Length - suffix.Length)
            return str;

        str = str.Remove(index);
        return str;
    }
}

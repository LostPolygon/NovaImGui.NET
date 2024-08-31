namespace NovaImGuiNet.Generator;

public static class StringUtility {
    public static string RemoveFromStart(this string str, string removed) {
        int index = str.IndexOf(removed, StringComparison.Ordinal);
        if (index != 0)
            return str;

        str = str.Remove(0, removed.Length);
        return str;
    }

    public static string RemoveFromEnd(this string str, string removed) {
        int index = str.LastIndexOf(removed, StringComparison.Ordinal);
        if (index == -1 || index != str.Length - removed.Length)
            return str;

        str = str.Remove(index);
        return str;
    }
}

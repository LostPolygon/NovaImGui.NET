namespace NovaImGuiNet.Generator.Metadata;

internal static class EqualityListExtensions {
    public static EqualityList<TSource> ToEqualityList<TSource>(this IEnumerable<TSource> source) {
        return new EqualityList<TSource>(source);
    }
}

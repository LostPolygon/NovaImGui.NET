namespace NovaImGuiNet.Generator;

public static class EqualityListExtensions {
    public static EqualityList<TSource> ToEqualityList<TSource>(this IEnumerable<TSource> source) {
        return new EqualityList<TSource>(source);
    }
}

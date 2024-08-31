namespace NovaImGuiNet.Generator.CSharp;

internal static class ListExtensions {
    public static void UpdateInPlace<TItem, TSearch>(
        this IList<TItem> list,
        Func<TSearch, IReadOnlyList<TItem>> itemTransformer
    ) where TSearch : TItem {
        UpdateInPlace<TItem, TSearch>(list, (item, _) => itemTransformer(item));
    }

    public static void UpdateInPlace<TItem, TSearch>(
        this IList<TItem> list,
        Func<TSearch, TItem> itemTransformer
    ) where TSearch : TItem {
        UpdateInPlace<TItem, TSearch>(list, x => [itemTransformer(x)]);
    }

    public static void UpdateInPlace<TItem, TSearch>(
        this IList<TItem> list,
        Func<TSearch, int, TItem> itemTransformer
    ) where TSearch : TItem {
        UpdateInPlace<TItem, TSearch>(list, (item, index) => [itemTransformer(item, index)]);
    }

    public static void UpdateInPlace<TItem, TSearch>(
        this IList<TItem> list,
        Func<TSearch, int, IReadOnlyList<TItem>> itemTransformer
    ) where TSearch : TItem {
        TSearch[] allInitialItems = list.OfType<TSearch>().ToArray();
        for (int index = 0; index < allInitialItems.Length; index++) {
            TSearch item = allInitialItems[index];
            IReadOnlyList<TItem> newItems = itemTransformer(item, index);
            list.UpdateInPlace(item, newItems);
        }
    }

    public static void UpdateInPlace<TItem>(
        this IList<TItem> list,
        TItem item,
        IReadOnlyList<TItem> replacementItems
    ) {
        if (replacementItems.Count == 1 && ReferenceEquals(item, replacementItems[0]))
            return;

        int initialItemIndex = list.IndexOf(item);
        if (initialItemIndex == -1)
            throw new InvalidOperationException($"Initial item {item} not found in list");

        list.RemoveAt(initialItemIndex);
        foreach (TItem newItem in replacementItems.Reverse()) {
            list.Insert(initialItemIndex, newItem);
        }
    }
}

using System.Collections;

namespace OpenSolarMax.Mods.Core.Utils;

internal class SingleItemGroup<TKey, TItem>(TKey key, TItem item) : IGrouping<TKey, TItem>
{
    public TKey Key => key;

    public IEnumerator<TItem> GetEnumerator() { yield return item; }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class EnumerableGroup<TKey, TItem>(TKey key, IEnumerable<TItem> items) : IGrouping<TKey, TItem>
{
    public TKey Key => key;

    public IEnumerator<TItem> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

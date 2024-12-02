using System.Collections;

namespace OpenSolarMax.Mods.Core.Utils;

internal class SingleItemGroup<TKey, TItem>(TKey key, TItem item) : IGrouping<TKey, TItem>
{
    public TKey Key => key;

    public IEnumerator<TItem> GetEnumerator() { yield return item; }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

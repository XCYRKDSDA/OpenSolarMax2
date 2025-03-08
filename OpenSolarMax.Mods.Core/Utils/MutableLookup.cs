using System.Collections;

namespace OpenSolarMax.Mods.Core.Utils;

public class MutableLookup<TKey, TElement> : Dictionary<TKey, List<TElement>>,
                                             ILookup<TKey, TElement> where TKey : notnull
{
    private class Group(TKey key, List<TElement> elements) : IGrouping<TKey, TElement>
    {
        IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator() => elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<TElement>)this).GetEnumerator();

        TKey IGrouping<TKey, TElement>.Key => key;
    }

    IEnumerator<IGrouping<TKey, TElement>>
        IEnumerable<IGrouping<TKey, TElement>>.GetEnumerator()
    {
        foreach (var (key, elements) in this)
        {
            if (elements.Count == 0)
                continue;

            yield return new Group(key, elements);
        }
    }

    bool ILookup<TKey, TElement>.Contains(TKey key)
    {
        if (TryGetValue(key, out var list))
            return list.Count == 0;
        return false;
    }

    IEnumerable<TElement> ILookup<TKey, TElement>.this[TKey key]
        => TryGetValue(key, out var list) ? list.Count == 0 ? [] : list : [];

    public ILookup<TKey, TElement> AsLookup() => this;
}

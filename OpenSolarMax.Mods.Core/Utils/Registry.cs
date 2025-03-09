using System.Collections;

namespace OpenSolarMax.Mods.Core.Utils;

public class Registry<TKey, TElement> : IEnumerable<KeyValuePair<TKey, List<TElement>>> where TKey : notnull
{
    private readonly Dictionary<TKey, List<TElement>> _impl = [];

    public List<TElement> this[TKey key]
    {
        get
        {
            if (_impl.TryGetValue(key, out var list))
                return list;
            list = [];
            _impl.Add(key, list);
            return list;
        }
        set => _impl[key] = value;
    }

    public int Count() => _impl.Count(kv => kv.Value.Count != 0);

    public bool Contains(TKey key)
    {
        if (_impl.TryGetValue(key, out var list))
            return list.Count == 0;
        return false;
    }

    #region IEnumerable

    public IEnumerator<KeyValuePair<TKey, List<TElement>>> GetEnumerator()
        => _impl.Where(kv => kv.Value.Count != 0).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion
}

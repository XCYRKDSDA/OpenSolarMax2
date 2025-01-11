using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Arch.Core;
using OpenSolarMax.Game.Data;

namespace OpenSolarMax.Game.Utils;

public class TemplateGroup(ITemplate[] templates)
    : ITemplate, IReadOnlyList<ITemplate>, IReadOnlyDictionary<Type, ITemplate>
{
    private readonly IReadOnlyList<ITemplate> _templates = templates;
    private readonly Dictionary<Type, ITemplate> _record = templates.Select(t => (t.GetType(), t)).ToDictionary();

    #region ITemplate

    public Archetype Archetype
    {
        get
        {
            var unionArchetype = new Archetype();
            foreach (var template in this)
                unionArchetype += template.Archetype;
            return unionArchetype;
        }
    }

    public void Apply(Entity entity)
    {
        foreach (var template in this)
            template.Apply(entity);
    }

    #endregion

    #region IReadOnlyList<ITemplate>

    public int Count => _templates.Count;

    public ITemplate this[int index] => _templates[index];

    public IEnumerator<ITemplate> GetEnumerator() => _templates.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _templates.GetEnumerator();

    #endregion

    #region IReadOnlyDictionary<Type, ITemplate>

    public bool ContainsKey(Type key) => _record.ContainsKey(key);

    public bool TryGetValue(Type key, [MaybeNullWhen(false)] out ITemplate value)
        => _record.TryGetValue(key, out value);

    public ITemplate this[Type key] => _record[key];

    public IEnumerable<Type> Keys => _record.Keys;

    public IEnumerable<ITemplate> Values => _record.Values;

    IEnumerator<KeyValuePair<Type, ITemplate>> IEnumerable<KeyValuePair<Type, ITemplate>>.GetEnumerator()
        => _record.GetEnumerator();

    #endregion

    public T Get<T>() where T : ITemplate => (T)this[typeof(T)];
}

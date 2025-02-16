using System.Diagnostics.CodeAnalysis;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Game.Modding;

public sealed class SystemCollection : Dictionary<Type, ISystem>, ISystemProvider
{
    public T Get<T>() where T : ISystem => (T)this[typeof(T)];

    public bool TryGet<T>([MaybeNullWhen(false)] out T system) where T : ISystem
    {
        var found = TryGetValue(typeof(T), out var systemObject);
        if (!found)
        {
            system = default;
            return false;
        }
        system = (T)systemObject!;
        return true;
    }
}

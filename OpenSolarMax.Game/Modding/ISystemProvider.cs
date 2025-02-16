using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Game.Modding;

public interface ISystemProvider
{
    T Get<T>() where T : ISystem;

    bool TryGet<T>(out T? system) where T : ISystem;
}

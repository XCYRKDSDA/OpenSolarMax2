using Arch.Core;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 星球之间的可达性查询结果
/// </summary>
public struct ReachabilityRegistry()
{
    public Dictionary<Entity, bool> FromHereTo = [];

    public Dictionary<Entity, bool> ToHereFrom = [];
}

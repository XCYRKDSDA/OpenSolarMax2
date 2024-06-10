using Arch.Core;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

[Component]
public readonly struct Battlefield()
{
    /// <summary>
    /// 阵营 -> 伤害
    /// </summary>
    public readonly Dictionary<EntityReference, float> FrontlineDamage = [];
}

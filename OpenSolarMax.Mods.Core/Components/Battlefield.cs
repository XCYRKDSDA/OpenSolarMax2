using Arch.Core;
using OpenSolarMax.Game.Modding;

namespace OpenSolarMax.Mods.Core.Components;

[Component]
public readonly struct Battlefield()
{
    /// <summary>
    /// 阵营 -> 伤害
    /// </summary>
    public readonly Dictionary<Entity, float> FrontlineDamage = [];
}

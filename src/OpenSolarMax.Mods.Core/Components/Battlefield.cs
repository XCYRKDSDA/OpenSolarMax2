using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Core.Components;

[Component]
public readonly struct Battlefield()
{
    /// <summary>
    /// 阵营 -> 伤害
    /// </summary>
    public readonly Dictionary<Entity, float> FrontlineDamage = [];

    /// <summary>
    /// 以预置的战损字典构造战场组件。仅供需要整体替换战损表（如经命令缓冲延迟写回）的场景使用
    /// </summary>
    public Battlefield(Dictionary<Entity, float> frontlineDamage)
        : this()
    {
        FrontlineDamage = frontlineDamage;
    }
}

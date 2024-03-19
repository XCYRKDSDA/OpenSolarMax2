using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 战斗能力组件。拥有该组件的阵营可以参与战斗。
/// 字段描述阵营的战斗能力
/// </summary>
[Component]
public struct Combatable
{
    /// <summary>
    /// 单位每秒造成的伤害值
    /// </summary>
    public float AttackPerUnitPerSecond;

    /// <summary>
    /// 每个单位最大可承受的伤害值
    /// </summary>
    public float MaximumDamagePerUnit;
}

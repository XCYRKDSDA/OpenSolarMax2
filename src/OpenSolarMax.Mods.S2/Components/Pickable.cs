using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 点选能力与配置
/// </summary>
[Component]
public struct Pickable
{
    /// <summary>
    /// 命中半径（世界系）
    /// </summary>
    public float HitRadius;
}

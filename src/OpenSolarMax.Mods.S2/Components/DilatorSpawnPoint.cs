using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 临时 dilator 生成点。记录触发阈值与出现时的舰队配置
/// </summary>
[Component]
public struct DilatorSpawnPoint
{
    /// <summary>
    /// 触发所需的人口容量阈值。任一阵营的人口容量高于该值才可能触发
    /// </summary>
    public int PopulationLimitThreshold;

    /// <summary>
    /// 触发所需的在场舰船数阈值。任一阵营的在场舰船数高于该值才可能触发
    /// </summary>
    public int CurrentPopulationThreshold;

    /// <summary>
    /// 出现时为临时 dilator 生成的舰船数
    /// </summary>
    public int ShipCount;

    /// <summary>
    /// 临时 dilator 与舰队所属的阵营
    /// </summary>
    public Entity Team;
}

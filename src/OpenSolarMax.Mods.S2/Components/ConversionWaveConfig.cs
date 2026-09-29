using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 转化波的配置：目标阵营、半径扩张速度与表现扩张范围的视觉子实体。创建后不再改动
/// </summary>
[Component]
public struct ConversionWaveConfig
{
    /// <summary>
    /// 转化波的目标阵营
    /// </summary>
    public Entity ConversionTeam;

    /// <summary>
    /// 半径的扩张速度
    /// </summary>
    public float ConversionSpeed;

    /// <summary>
    /// 表现扩张范围的视觉子实体
    /// </summary>
    public Entity Visual;
}

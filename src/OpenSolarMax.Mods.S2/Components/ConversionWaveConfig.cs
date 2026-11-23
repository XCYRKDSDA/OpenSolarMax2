using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 转化波的配置
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
}

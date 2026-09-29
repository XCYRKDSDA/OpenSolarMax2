using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 转化 dilator 的配置：转化波的目标阵营与半径扩张速度。创建后不再改动
/// </summary>
[Component]
public struct ConvertingDilatorConfig
{
    /// <summary>
    /// 转化波的目标阵营
    /// </summary>
    public Entity ConversionTeam;

    /// <summary>
    /// 转化波半径的扩张速度
    /// </summary>
    public float ConversionSpeed;
}

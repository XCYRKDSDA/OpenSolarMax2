using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Utils;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 产能组件。描述星球的生产能力
/// </summary>
[Component]
public struct ProductionAbility
{
    /// <summary>
    /// 星球提供的人口数目
    /// </summary>
    public int Population;

    /// <summary>
    /// 星球的生产速度
    /// </summary>
    public float ProgressPerSecond;
}

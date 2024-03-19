using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Utils;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 可生产组件。
/// 拥有该组件的阵营可以进行生产。
/// 字段描述生产一个该阵营单位需要的工作量
/// </summary>
[Component]
public struct Producible
{
    /// <summary>
    /// 生产该类型单位需要的工作量
    /// </summary>
    public float WorkloadPerShip;
}

/// <summary>
/// 产能组件。描述星球的生产能力
/// </summary>
[Component]
public struct ProductionAbility
{
    /// <summary>
    /// 星球可以生产的单位类型。由一组模板来描述
    /// </summary>
    public ITemplate[] ProductTemplates;

    /// <summary>
    /// 星球提供的人口数目
    /// </summary>
    public int Population;

    /// <summary>
    /// 星球的生产速度
    /// </summary>
    public float ProgressPerSecond;
}

/// <summary>
/// 生产状态组件。描述星球当前的生产进度
/// </summary>
[Component]
public struct ProductionState
{
    /// <summary>
    /// 当前单位的生产进度
    /// </summary>
    public float Progress;
}

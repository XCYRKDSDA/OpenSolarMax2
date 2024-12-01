using Arch.Core;
using Microsoft.Xna.Framework;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 运输任务组件。描述某个单位参与的运输任务
/// </summary>
public struct ShippingTask
{
    /// <summary>
    /// 当前运输的目标星球
    /// </summary>
    public EntityReference DestinationPlanet;

    /// <summary>
    /// 开始运输时的位置
    /// </summary>
    public Vector3 DeparturePosition;

    /// <summary>
    /// 预计抵达星球时的目标位置
    /// </summary>
    public Vector3 ExpectedArrivalPosition;

    /// <summary>
    /// 预计飞行时间
    /// </summary>
    public float ExpectedTravelDuration;

    /// <summary>
    /// 预计所泊入的轨道
    /// </summary>
    public RevolutionOrbit ExpectedRevolutionOrbit;

    /// <summary>
    /// 预计入轨时的状态
    /// </summary>
    public RevolutionState ExpectedRevolutionState;
}

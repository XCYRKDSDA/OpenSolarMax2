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
    public Entity DestinationPlanet;

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

public struct ShippingStatus_Charging
{
    /// <summary>
    /// 已充能的时间
    /// </summary>
    public float ElapsedTime;
}

public struct ShippingStatus_Travelling
{
    /// <summary>
    /// 由于充能耽搁的时间
    /// </summary>
    public float DelayedTime;

    /// <summary>
    /// 已经飞行了的时间
    /// </summary>
    public float ElapsedTime;
}

public enum ShippingState
{
    Idle,
    Charging,
    Travelling,
}

public struct ShippingStatus
{
    /// <summary>
    /// 当前飞行状态机状态
    /// </summary>
    public ShippingState State;

    /// <summary>
    /// 当前飞行任务
    /// </summary>
    public ShippingTask Task;

    /// <summary>
    /// 处于<see cref="ShippingState.Charging"/>状态时的具体状态
    /// </summary>
    public ShippingStatus_Charging Charging;

    /// <summary>
    /// 处于<see cref="ShippingState.Travelling"/>状态时的具体状态
    /// </summary>
    public ShippingStatus_Travelling Travelling;
}

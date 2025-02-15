using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 传送任务组件。描述某个单位参与的传送任务
/// </summary>
public struct TransportingTask
{
    /// <summary>
    /// 当前传送的目标星球
    /// </summary>
    public EntityReference DestinationPlanet;

    /// <summary>
    /// 预计泊入的轨道
    /// </summary>
    public RevolutionOrbit ExpectedRevolutionOrbit;

    /// <summary>
    /// 预计入轨时的公转状态
    /// </summary>
    public RevolutionState ExpectedRevolutionState;
}

public enum TransportingState
{
    Idle,
    PreTransportation,
    PostTransportation
}

public struct TransportingStatus_PreTransportation
{
    public TimeSpan ElapsedTime;
}

public struct TransportingStatus_PostTransportation
{
    public TimeSpan ElapsedTime;
}

public struct TransportingStatus
{
    /// <summary>
    /// 当前传送状态机状态
    /// </summary>
    public TransportingState State;

    /// <summary>
    /// 当前传送任务
    /// </summary>
    public TransportingTask Task;

    /// <summary>
    /// 处于<see cref="TransportingState.PreTransportation"/>状态时的具体状态
    /// </summary>
    public TransportingStatus_PreTransportation PreTransportation;

    /// <summary>
    /// 处于<see cref="TransportingState.PostTransportation"/>状态时的具体状态
    /// </summary>
    public TransportingStatus_PostTransportation PostTransportation;
}

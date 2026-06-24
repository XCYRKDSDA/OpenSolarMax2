using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 传送任务组件。描述某个舰船参与的传送任务
/// </summary>
public struct WarpingTask
{
    /// <summary>
    /// 当前传送的目标星球
    /// </summary>
    public Entity DestinationPlanet;

    /// <summary>
    /// 预计泊入的轨道
    /// </summary>
    public RevolutionOrbit ExpectedRevolutionOrbit;

    /// <summary>
    /// 预计入轨时的公转状态
    /// </summary>
    public RevolutionState ExpectedRevolutionState;
}

public enum WarpingState
{
    Idle,
    PreWarp,
    PostWarp,
}

public struct WarpingStatus_PreWarp
{
    public TimeSpan ElapsedTime;
}

public struct WarpingStatus_PostWarp
{
    public TimeSpan ElapsedTime;
}

public struct WarpingStatus
{
    /// <summary>
    /// 当前传送状态机状态
    /// </summary>
    public WarpingState State;

    /// <summary>
    /// 当前传送任务
    /// </summary>
    public WarpingTask Task;

    /// <summary>
    /// 处于<see cref="WarpingState.PreWarp"/>状态时的具体状态
    /// </summary>
    public WarpingStatus_PreWarp PreWarp;

    /// <summary>
    /// 处于<see cref="WarpingState.PostWarp"/>状态时的具体状态
    /// </summary>
    public WarpingStatus_PostWarp PostWarp;
}

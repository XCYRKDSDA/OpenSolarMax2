using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
///  Unit 当前所处的死亡阶段
/// </summary>
public enum DeathState
{
    /// <summary>
    /// 存活，正常参与游戏逻辑
    /// </summary>
    Alive,

    /// <summary>
    /// 濒死，停止参与游戏逻辑，等待播放死亡特效
    /// </summary>
    Dying,

    /// <summary>
    /// 已死亡，特效已播放完毕，等待销毁
    /// </summary>
    Dead,
}

/// <summary>
///  Unit 死亡状态组件。
/// 所有 Unit 出生时即携带此组件，<see cref="State"/> 初始为 <see cref="DeathState.Alive"/>
/// </summary>
[Component]
public struct UnitDeathState
{
    public DeathState State;
}

using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 胜利退出计时器组件。
/// 拥有该组件的实体在胜利后开始倒计时，归零时触发关卡退出
/// </summary>
[Component]
public struct VictoryExitTimer : ICountDownTimer
{
    /// <summary>
    /// 剩余时间
    /// </summary>
    public TimeSpan TimeLeft { get; set; }
}

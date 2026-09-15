using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 待触发的胜利波纹效果计时器。
/// 实现 ICountDownTimer 供 CountDownSystemBase 倒数。
/// 由 GameOverSystem 在胜利帧为每颗星球创建。
/// </summary>
[Component]
public struct PendingVictoryEffect : ICountDownTimer
{
    public TimeSpan TimeLeft { get; set; }
}

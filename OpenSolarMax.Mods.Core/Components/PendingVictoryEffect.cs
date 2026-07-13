using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 待触发的胜利波纹效果。
/// 拥有该组件的实体在倒计时归零时触发单颗星球的"爆炸+改阵营+重置殖民"全套动作。
/// 由 GameOverSystem 在胜利帧为每颗星球创建。
/// </summary>
public struct PendingVictoryEffect() : ICountDownTimer
{
    /// <summary>
    /// 剩余时间
    /// </summary>
    public TimeSpan TimeLeft { get; set; }

    /// <summary>
    /// 要操作的星球实体
    /// </summary>
    public Entity Planet = Entity.Null;

    /// <summary>
    /// 获胜方阵营实体
    /// </summary>
    public Entity Winner = Entity.Null;
}

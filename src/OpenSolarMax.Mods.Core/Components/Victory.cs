using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 标记一个队伍实体并追踪其是否已获胜。
/// 初始为 <see cref="HasWon"/> = false，由 <see cref="DetectVictorySystem"/> 在胜利条件满足时设为 true
/// </summary>
[Component]
public struct Victory
{
    public bool HasWon;
}

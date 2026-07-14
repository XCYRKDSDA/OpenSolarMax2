using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 胜利波纹效果的目标星球和获胜方。
/// 与 PendingVictoryEffect 计时器配套使用。
/// </summary>
[Relationship]
public partial struct VictoryEffectTarget()
{
    /// <summary>
    /// 要操作的星球实体
    /// </summary>
    [Participant]
    public Entity Planet = Entity.Null;

    /// <summary>
    /// 获胜方阵营实体
    /// </summary>
    [Participant]
    public Entity Winner = Entity.Null;
}

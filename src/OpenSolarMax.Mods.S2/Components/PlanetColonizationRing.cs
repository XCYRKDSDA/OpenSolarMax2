using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 殖民环的弧段
/// </summary>
public enum ColonizationRingPart
{
    /// <summary>
    /// 已殖民进度
    /// </summary>
    Progress,

    /// <summary>
    /// 尚未殖民的部分
    /// </summary>
    Remainder,
}

/// <summary>
/// 天体与殖民进度环的关系
/// </summary>
[Relationship(generateConcept: true)]
public readonly partial struct PlanetColonizationRing(
    Entity planet,
    Entity progress,
    Entity remainder
)
{
    /// <summary>
    /// 天体实体。非独占：每个视图一条关系。
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Planet = planet;

    /// <summary>
    /// 已殖民进度的弧段。独占：一个弧段只属于一个天体。
    /// </summary>
    [Participant]
    public readonly Entity Progress = progress;

    /// <summary>
    /// 尚未殖民的弧段。独占：一个弧段只属于一个天体。
    /// </summary>
    [Participant]
    public readonly Entity Remainder = remainder;
}

using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 被点选实体与悬停发光的关系
/// </summary>
[Relationship(generateConcept: true)]
public readonly partial struct PointedEntityHoverGlow(
    Entity pointedEntity,
    Entity glow,
    Entity view
)
{
    /// <summary>
    /// 被点选实体。非独占：同一实体可以在多个视图中被悬停。
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity PointedEntity = pointedEntity;

    /// <summary>
    /// 悬停发光实体。独占：一个发光只属于一个被点选实体。
    /// </summary>
    [Participant]
    public readonly Entity Glow = glow;

    /// <summary>
    /// 所属视图。非独占：一个视图同一时刻只有一个悬停发光。
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity View = view;
}

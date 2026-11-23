using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 世界实体到屏幕实体的投影关系
/// </summary>
[Relationship(generateConcept: true)]
public readonly partial struct WorldToScreenProjection(
    Entity source,
    Entity target,
    Entity view,
    bool projectRotation
)
{
    [Participant(exclusive: false)]
    public readonly Entity Source = source;

    [Participant]
    public readonly Entity Target = target;

    [Participant(exclusive: false)]
    public readonly Entity View = view;

    /// <summary>
    /// 是否同时投影世界实体的旋转，而不仅是位置
    /// </summary>
    public readonly bool ProjectRotation = projectRotation;
}

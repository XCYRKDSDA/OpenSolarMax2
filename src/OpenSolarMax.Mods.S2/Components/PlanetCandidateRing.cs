using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 星球与候选选择圈的一对一关系。一个星球可以有多个候选圈（多视图场景），一个候选圈只属于一个星球。
/// </summary>
[Relationship]
public readonly partial struct PlanetCandidateRing(Entity planet, Entity ring, Entity view)
{
    /// <summary>
    /// 星球实体。非独占：一个星球可以被多个视图同时作为候选，因此可以有多个候选圈。
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Planet = planet;

    /// <summary>
    /// 候选圈实体。独占：一个候选圈只属于一个星球。
    /// </summary>
    [Participant]
    public readonly Entity Ring = ring;

    /// <summary>
    /// 所属视图。非独占：一个视图可以显示多个候选圈。
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity View = view;
}

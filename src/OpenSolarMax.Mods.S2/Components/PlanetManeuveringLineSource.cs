using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 出发星球与拖拽线段的对应关系
/// </summary>
[Relationship(generateConcept: true)]
public readonly partial struct PlanetManeuveringLineSource(Entity planet, Entity line, Entity view)
{
    /// <summary>
    /// 出发星球实体。非独占：一个星球可以有多条线段
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Planet = planet;

    /// <summary>
    /// 线段实体。独占：一条线段只属于一个星球
    /// </summary>
    [Participant]
    public readonly Entity Line = line;

    /// <summary>
    /// 所属视图。非独占：一个视图可以显示多条线段
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity View = view;
}

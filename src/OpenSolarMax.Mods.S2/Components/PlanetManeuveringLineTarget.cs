using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 目标星球与拖拽线段的对应关系
/// </summary>
[Relationship(generateConcept: true)]
public readonly partial struct PlanetManeuveringLineTarget(Entity planet, Entity line, Entity view)
{
    /// <summary>
    /// 目标星球实体。非独占：一个星球可以被多条线段指向
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Planet = planet;

    /// <summary>
    /// 线段实体。独占：一条线段只有一个目标
    /// </summary>
    [Participant]
    public readonly Entity Line = line;

    /// <summary>
    /// 所属视图。非独占：一个视图可以显示多条线段
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity View = view;
}

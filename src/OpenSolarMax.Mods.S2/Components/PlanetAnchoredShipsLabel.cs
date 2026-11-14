using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 天体与舰船数量标签的关系
/// </summary>
[Relationship(generateConcept: true)]
public readonly partial struct PlanetAnchoredShipsLabel(Entity planet, Entity label, Entity team)
{
    /// <summary>
    /// 天体实体。非独占：一个天体可以有多个阵营的标签。
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Planet = planet;

    /// <summary>
    /// 标签实体。独占：一个标签只属于一个天体。
    /// </summary>
    [Participant]
    public readonly Entity Label = label;

    /// <summary>
    /// 对应的阵营
    /// </summary>
    public readonly Entity Team = team;
}

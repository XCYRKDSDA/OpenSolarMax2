using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 天体与舰船构成弧的关系
/// </summary>
[Relationship(generateConcept: true)]
public readonly partial struct PlanetAnchoredShipsArc(Entity planet, Entity arc, Entity team)
{
    /// <summary>
    /// 天体实体。非独占：一个天体可以有多条构成弧。
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Planet = planet;

    /// <summary>
    /// 构成弧实体。独占：一条构成弧只属于一个天体。
    /// </summary>
    [Participant]
    public readonly Entity Arc = arc;

    /// <summary>
    /// 对应的阵营
    /// </summary>
    public readonly Entity Team = team;
}

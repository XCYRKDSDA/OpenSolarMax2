using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 天体之间的可达性关系。描述一个「从出发天体前往目的天体」的有向关系
/// </summary>
[Relationship]
public readonly partial struct ReachabilityRelation(Entity departure, Entity destination)
{
    /// <summary>
    /// 出发天体实体。一个天体可以同时作为多条可达性关系的出发端
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Departure = departure;

    /// <summary>
    /// 目的天体实体。一个天体可以同时作为多条可达性关系的目的端
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Destination = destination;
}

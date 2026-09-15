using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, Reactive]
public sealed class IndexAnchorageSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<TreeRelationship<Anchorage>>(registry) { }

[SimulateSystem, Reactive]
public sealed class IndexTrailAffiliationSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<TrailOf>(registry) { }

/// <summary>
/// 索引星球与选择圈的关系，维护 AsPlanet 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, Reactive]
public sealed class IndexPlanetSelectionRingSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<PlanetSelectionRing>(registry) { }

/// <summary>
/// 索引视图与选择圈的关系，维护 AsView 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, Reactive]
public sealed class IndexViewSelectionRingSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<ViewSelectionRing>(registry) { }

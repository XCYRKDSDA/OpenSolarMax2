using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, Reactive]
public sealed class IndexDependenceSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<Dependence>(registry) { }

[SimulateSystem, Reactive, BothForGameplayAndPreview]
public sealed class IndexTeamAffiliationSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<InTeam>(registry) { }

[SimulateSystem, Reactive]
public sealed class IndexAnchorageSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<TreeRelationship<Anchorage>>(registry) { }

[SimulateSystem, Reactive, BothForGameplayAndPreview]
public sealed class IndexTransformTreeSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<TreeRelationship<RelativeTransform>>(registry) { }

[SimulateSystem, Reactive]
public sealed class IndexTrailAffiliationSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<TrailOf>(registry) { }

[SimulateSystem, Reactive, BothForGameplayAndPreview]
public sealed class IndexColorSyncTreeSystem(EventRegistry registry)
    : IndexRelationshipSystemBase<TreeRelationship<ColorSync>>(registry) { }

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

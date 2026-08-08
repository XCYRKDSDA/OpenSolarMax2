using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, Reactive]
public sealed class IndexDependenceSystem(World world)
    : IndexRelationshipSystemBase<Dependence>(world) { }

[SimulateSystem, Reactive, BothForGameplayAndPreview]
public sealed class IndexTeamAffiliationSystem(World world)
    : IndexRelationshipSystemBase<InTeam>(world) { }

[SimulateSystem, Reactive]
public sealed class IndexAnchorageSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<Anchorage>>(world) { }

[SimulateSystem, Reactive, BothForGameplayAndPreview]
public sealed class IndexTransformTreeSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<RelativeTransform>>(world) { }

[SimulateSystem, Reactive]
public sealed class IndexTrailAffiliationSystem(World world)
    : IndexRelationshipSystemBase<TrailOf>(world) { }

[SimulateSystem, Reactive, BothForGameplayAndPreview]
public sealed class IndexColorSyncTreeSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<ColorSync>>(world) { }

/// <summary>
/// 索引星球与选择圈的关系，维护 AsPlanet 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, Reactive]
public sealed class IndexPlanetSelectionRingSystem(World world)
    : IndexRelationshipSystemBase<PlanetSelectionRing>(world) { }

/// <summary>
/// 索引视图与选择圈的关系，维护 AsView 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, Reactive]
public sealed class IndexViewSelectionRingSystem(World world)
    : IndexRelationshipSystemBase<ViewSelectionRing>(world) { }

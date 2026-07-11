using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[
    ReadCurr(typeof(Dependence)),
    Write(typeof(Dependence.AsDependent)),
    Write(typeof(Dependence.AsDependency))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexDependenceSystem(World world)
    : IndexRelationshipSystemBase<Dependence>(world) { }

[SimulateSystem, AfterStructuralChanges, BothForGameplayAndPreview]
[ReadCurr(typeof(InTeam)), Write(typeof(InTeam.AsTeam)), Write(typeof(InTeam.AsAffiliate))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTeamAffiliationSystem(World world)
    : IndexRelationshipSystemBase<InTeam>(world) { }

[SimulateSystem, AfterStructuralChanges]
[
    ReadCurr(typeof(TreeRelationship<Anchorage>)),
    Write(typeof(TreeRelationship<Anchorage>.AsParent)),
    Write(typeof(TreeRelationship<Anchorage>.AsChild))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexAnchorageSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<Anchorage>>(world) { }

[SimulateSystem, AfterStructuralChanges, BothForGameplayAndPreview]
[
    ReadCurr(typeof(TreeRelationship<RelativeTransform>)),
    Write(typeof(TreeRelationship<RelativeTransform>.AsParent)),
    Write(typeof(TreeRelationship<RelativeTransform>.AsChild))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTransformTreeSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<RelativeTransform>>(world) { }

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TrailOf)), Write(typeof(TrailOf.AsShip)), Write(typeof(TrailOf.AsTrail))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTrailAffiliationSystem(World world)
    : IndexRelationshipSystemBase<TrailOf>(world) { }

[SimulateSystem, AfterStructuralChanges, BothForGameplayAndPreview]
[
    ReadCurr(typeof(TreeRelationship<ColorSync>)),
    Write(typeof(TreeRelationship<ColorSync>.AsParent)),
    Write(typeof(TreeRelationship<ColorSync>.AsChild))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexColorSyncTreeSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<ColorSync>>(world) { }

/// <summary>
/// 索引星球与选择圈的关系，维护 AsPlanet 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, AfterStructuralChanges]
[
    ReadCurr(typeof(PlanetSelectionRing)),
    Write(typeof(PlanetSelectionRing.AsPlanet)),
    Write(typeof(PlanetSelectionRing.AsRing))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexPlanetSelectionRingSystem(World world)
    : IndexRelationshipSystemBase<PlanetSelectionRing>(world) { }

/// <summary>
/// 索引视图与选择圈的关系，维护 AsView 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, AfterStructuralChanges]
[
    ReadCurr(typeof(ViewSelectionRing)),
    Write(typeof(ViewSelectionRing.AsView)),
    Write(typeof(ViewSelectionRing.AsRing))
]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexViewSelectionRingSystem(World world)
    : IndexRelationshipSystemBase<ViewSelectionRing>(world) { }

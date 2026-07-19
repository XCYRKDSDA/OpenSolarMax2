using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, PostUpdate]
[
    ReadCurr(typeof(Dependence)),
    Write(typeof(Dependence.AsDependent)),
    Write(typeof(Dependence.AsDependency))
]
[Disable]
public sealed class IndexDependenceSystem(World world)
    : IndexRelationshipSystemBase<Dependence>(world) { }

[SimulateSystem, PostUpdate, BothForGameplayAndPreview]
[ReadCurr(typeof(InTeam)), Write(typeof(InTeam.AsTeam)), Write(typeof(InTeam.AsAffiliate))]
[Disable]
public sealed class IndexTeamAffiliationSystem(World world)
    : IndexRelationshipSystemBase<InTeam>(world) { }

[SimulateSystem, PostUpdate]
[
    ReadCurr(typeof(TreeRelationship<Anchorage>)),
    Write(typeof(TreeRelationship<Anchorage>.AsParent)),
    Write(typeof(TreeRelationship<Anchorage>.AsChild))
]
[Disable]
public sealed class IndexAnchorageSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<Anchorage>>(world) { }

[SimulateSystem, PostUpdate, BothForGameplayAndPreview]
[
    ReadCurr(typeof(TreeRelationship<RelativeTransform>)),
    Write(typeof(TreeRelationship<RelativeTransform>.AsParent)),
    Write(typeof(TreeRelationship<RelativeTransform>.AsChild))
]
public sealed class IndexTransformTreeSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<RelativeTransform>>(world) { }

[SimulateSystem, PostUpdate]
[ReadCurr(typeof(TrailOf)), Write(typeof(TrailOf.AsShip)), Write(typeof(TrailOf.AsTrail))]
[Disable]
public sealed class IndexTrailAffiliationSystem(World world)
    : IndexRelationshipSystemBase<TrailOf>(world) { }

[SimulateSystem, PostUpdate, BothForGameplayAndPreview]
[
    ReadCurr(typeof(TreeRelationship<ColorSync>)),
    Write(typeof(TreeRelationship<ColorSync>.AsParent)),
    Write(typeof(TreeRelationship<ColorSync>.AsChild))
]
[Disable]
public sealed class IndexColorSyncTreeSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<ColorSync>>(world) { }

/// <summary>
/// 索引星球与选择圈的关系，维护 AsPlanet 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, PostUpdate]
[
    ReadCurr(typeof(PlanetSelectionRing)),
    Write(typeof(PlanetSelectionRing.AsPlanet)),
    Write(typeof(PlanetSelectionRing.AsRing))
]
[Disable]
public sealed class IndexPlanetSelectionRingSystem(World world)
    : IndexRelationshipSystemBase<PlanetSelectionRing>(world) { }

/// <summary>
/// 索引视图与选择圈的关系，维护 AsView 和 AsRing 索引组件。
/// </summary>
[SimulateSystem, PostUpdate]
[
    ReadCurr(typeof(ViewSelectionRing)),
    Write(typeof(ViewSelectionRing.AsView)),
    Write(typeof(ViewSelectionRing.AsRing))
]
[Disable]
public sealed class IndexViewSelectionRingSystem(World world)
    : IndexRelationshipSystemBase<ViewSelectionRing>(world) { }

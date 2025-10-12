using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(Dependence)), Write(typeof(Dependence.AsDependent)), Write(typeof(Dependence.AsDependency))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexDependenceSystem(World world)
    : IndexRelationshipSystemBase<Dependence>(world)
{ }

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(InParty)), Write(typeof(InParty.AsParty)), Write(typeof(InParty.AsAffiliate))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexPartyAffiliationSystem(World world)
    : IndexRelationshipSystemBase<InParty>(world)
{ }

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TreeRelationship<Anchorage>)), Write(typeof(TreeRelationship<Anchorage>.AsParent)),
 Write(typeof(TreeRelationship<Anchorage>.AsChild))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexAnchorageSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<Anchorage>>(world)
{ }

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TreeRelationship<RelativeTransform>)), Write(typeof(TreeRelationship<RelativeTransform>.AsParent)),
 Write(typeof(TreeRelationship<RelativeTransform>.AsChild))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTransformTreeSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<RelativeTransform>>(world)
{ }

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TrailOf)), Write(typeof(TrailOf.AsShip)), Write(typeof(TrailOf.AsTrail))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTrailAffiliationSystem(World world)
    : IndexRelationshipSystemBase<TrailOf>(world)
{ }

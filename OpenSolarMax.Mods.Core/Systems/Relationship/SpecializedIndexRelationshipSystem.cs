using Arch.Core;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, Stage2]
[Read(typeof(Dependence), withEntities: true)]
[Write(typeof(Dependence.AsDependent), withEntities: true)]
[Write(typeof(Dependence.AsDependency), withEntities: true)]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexDependenceSystem(World world)
    : IndexRelationshipSystemBase<Dependence>(world)
{ }

[SimulateSystem, Stage2]
[Read(typeof(InParty), withEntities: true)]
[Write(typeof(InParty.AsParty), withEntities: true)]
[Write(typeof(InParty.AsAffiliate), withEntities: true)]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexPartyAffiliationSystem(World world)
    : IndexRelationshipSystemBase<InParty>(world)
{ }

[SimulateSystem, Stage2]
[Read(typeof(TreeRelationship<Anchorage>), withEntities: true)]
[Write(typeof(TreeRelationship<Anchorage>.AsParent), withEntities: true)]
[Write(typeof(TreeRelationship<Anchorage>.AsChild), withEntities: true)]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexAnchorageSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<Anchorage>>(world)
{ }

[SimulateSystem, Stage2]
[Read(typeof(TreeRelationship<RelativeTransform>), withEntities: true)]
[Write(typeof(TreeRelationship<RelativeTransform>.AsParent), withEntities: true)]
[Write(typeof(TreeRelationship<RelativeTransform>.AsChild), withEntities: true)]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTransformTreeSystem(World world)
    : IndexRelationshipSystemBase<TreeRelationship<RelativeTransform>>(world)
{ }

[SimulateSystem, Stage2]
[Read(typeof(TrailOf), withEntities: true)]
[Write(typeof(TrailOf.AsShip), withEntities: true)]
[Write(typeof(TrailOf.AsTrail), withEntities: true)]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class IndexTrailAffiliationSystem(World world)
    : IndexRelationshipSystemBase<TrailOf>(world)
{ }

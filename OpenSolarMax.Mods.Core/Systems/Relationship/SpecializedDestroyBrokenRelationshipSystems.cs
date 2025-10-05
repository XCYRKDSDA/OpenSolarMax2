using Arch.Core;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem]
[Read(typeof(InParty), withEntities: true)]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenPartyRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<InParty>(world)
{ }

[SimulateSystem]
[Read(typeof(TreeRelationship<Anchorage>), withEntities: true)]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenAnchorageRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Anchorage>>(world)
{ }

[SimulateSystem]
[Read(typeof(TreeRelationship<RelativeTransform>), withEntities: true)]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenTransformRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<RelativeTransform>>(world)
{ }

[SimulateSystem]
[Read(typeof(TrailOf), withEntities: true)]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenTrailRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TrailOf>(world)
{ }

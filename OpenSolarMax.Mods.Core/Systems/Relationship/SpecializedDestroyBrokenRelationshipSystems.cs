using Arch.Core;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, Stage2]
[Read(typeof(InParty), withEntities: true)]
[DestroyEntities]
public sealed class DestroyBrokenPartyRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<InParty>(world)
{ }

[SimulateSystem, Stage2]
[Read(typeof(TreeRelationship<Anchorage>), withEntities: true)]
[DestroyEntities]
public sealed class DestroyBrokenAnchorageRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Anchorage>>(world)
{ }

[SimulateSystem, Stage2]
[Read(typeof(TreeRelationship<RelativeTransform>), withEntities: true)]
[DestroyEntities]
public sealed class DestroyBrokenTransformRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<RelativeTransform>>(world)
{ }

[SimulateSystem, Stage2]
[Read(typeof(TrailOf), withEntities: true)]
[DestroyEntities]
public sealed class DestroyBrokenTrailRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TrailOf>(world)
{ }

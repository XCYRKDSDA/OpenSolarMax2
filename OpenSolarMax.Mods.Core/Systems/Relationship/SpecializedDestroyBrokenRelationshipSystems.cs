using Arch.Core;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, ReactToStructuralChanges]
[ReadCurr(typeof(InParty), withEntities: true), ChangeStructure]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenPartyRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<InParty>(world)
{ }

[SimulateSystem, ReactToStructuralChanges]
[ReadCurr(typeof(TreeRelationship<Anchorage>), withEntities: true), ChangeStructure]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenAnchorageRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Anchorage>>(world)
{ }

[SimulateSystem, ReactToStructuralChanges]
[ReadCurr(typeof(TreeRelationship<RelativeTransform>), withEntities: true), ChangeStructure]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenTransformRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<RelativeTransform>>(world)
{ }

[SimulateSystem, ReactToStructuralChanges]
[ReadCurr(typeof(TrailOf), withEntities: true), ChangeStructure]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed class DestroyBrokenTrailRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TrailOf>(world)
{ }

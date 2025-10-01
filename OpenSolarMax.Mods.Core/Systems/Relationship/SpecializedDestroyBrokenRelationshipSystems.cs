using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[ReactivelyStructuralChangeSystem]
public sealed class DestroyBrokenPartyRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<InParty>(world), ISystem
{ }

[ReactivelyStructuralChangeSystem]
public sealed class DestroyBrokenAnchorageRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Anchorage>>(world), ISystem
{ }

[ReactivelyStructuralChangeSystem]
public sealed class DestroyBrokenTransformRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<RelativeTransform>>(world), ISystem
{ }

[ReactivelyStructuralChangeSystem]
public sealed class DestroyBrokenTrailRelationshipSystem(World world)
    : DestroyBrokenRelationshipsSystem<TrailOf>(world), ISystem
{ }

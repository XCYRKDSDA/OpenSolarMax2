using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[ReactivelyStructuralChangeSystem]
public sealed class DestroyBrokenPartyRelationshipSystem(World world, IAssetsManager assets)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Party>>(world), ISystem
{ }

[ReactivelyStructuralChangeSystem]
public sealed class DestroyBrokenAnchorageRelationshipSystem(World world, IAssetsManager assets)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Anchorage>>(world), ISystem
{ }

[ReactivelyStructuralChangeSystem]
public sealed class DestroyBrokenTransformRelationshipSystem(World world, IAssetsManager assets)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<RelativeTransform>>(world), ISystem
{ }

[ReactivelyStructuralChangeSystem]
public sealed class DestroyBrokenTrailRelationshipSystem(World world, IAssetsManager assets)
    : DestroyBrokenRelationshipsSystem<TrailOf>(world), ISystem
{ }

using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[StructuralChangeSystem]
public sealed class DestroyBrokenPartyRelationshipSystem(World world, IAssetsManager assets)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Party>>(world), ISystem
{ }

[StructuralChangeSystem]
public sealed class DestroyBrokenAnchorageRelationshipSystem(World world, IAssetsManager assets)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Anchorage>>(world), ISystem
{ }

[StructuralChangeSystem]
public sealed class DestroyBrokenTransformRelationshipSystem(World world, IAssetsManager assets)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<RelativeTransform>>(world), ISystem
{ }

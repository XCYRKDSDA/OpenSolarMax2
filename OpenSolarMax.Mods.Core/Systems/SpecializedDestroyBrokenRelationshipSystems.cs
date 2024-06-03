using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core;

[StructuralChangeSystem]
public sealed class DestroyBrokenPartyRelationshipSystem(World world, IAssetsManager assets)
    : DestroyBrokenRelationshipsSystem<TreeRelationship<Party>>(world), ISystem
{ }

using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Systems;

namespace OpenSolarMax.Mods.Core;

[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
public sealed class IndexDependenceSystem(World world, IAssetsManager assets)
    : IndexRelationshipSystem<Dependence>(world)
{ }

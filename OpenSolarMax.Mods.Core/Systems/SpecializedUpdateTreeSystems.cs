using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
public sealed class UpdateTransformTreeSystems(World world, IAssetsManager assets)
    : UpdateTreeSystem<RelativeTransform>(world, assets)
{ }

[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
public sealed class UpdateAnchorageTreeSystem(World world, IAssetsManager assets)
    : UpdateTreeSystem<Anchorage>(world, assets)
{ }

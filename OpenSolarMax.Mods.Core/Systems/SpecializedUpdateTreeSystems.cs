using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.System;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
public sealed class UpdateTransformTreeSystems(World world, IAssetsManager assets)
    : UpdateTreeSystem<RelativeTransform>(world, assets)
{ }

using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;
using Anchorage = OpenSolarMax.Mods.Core.Components.Anchorage;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteAfter(typeof(UpdateAnchorageTreeSystem))]
public sealed partial class UpdateShipRegistrySystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<Tree<Anchorage>.Parent, AnchoredShipsRegistry>]
    private static void CountAnchoredShips(in Tree<Anchorage>.Parent anchorageRelationship, ref AnchoredShipsRegistry shipRegistry)
    {
        shipRegistry.Ships = (Lookup<Entity, Entity>)anchorageRelationship._children.ToLookup((e) => e.Get<TreeRelationship<Party>.AsChild>().Index.Parent);
    }
}

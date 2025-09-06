using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[Disable]
[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyPartyColorSystem))]
[ExecuteAfter(typeof(UpdateShippingEffectSystem))]
[ExecuteAfter(typeof(GetShippingUnitsInRangeSystem))]
public sealed partial class ColorInRangeShipsSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<InAttackRangeShipsRegistry>]
    private static void SetColor(in InAttackRangeShipsRegistry registry)
    {
        foreach (var (_, pairs) in registry.Ships)
        {
            foreach (var (ship, _) in pairs)
            {
                ship.Get<Sprite>().Color = Color.Red;
            }
        }
    }
}

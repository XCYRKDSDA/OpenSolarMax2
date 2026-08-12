using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[Disable]
[SimulateSystem, LateUpdate]
[ReadCurr(typeof(InAttackRangeShipsRegistry)), Write(typeof(Sprite))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
// 在其他设置外观的系统之后执行以覆写
[
    ExecuteAfter(typeof(ApplyTeamColorSystem)), // Write Sprite
    ExecuteAfter(typeof(ApplyShipPostBornEffectSystem)), // Write Sprite
    ExecuteAfter(typeof(UpdateShipChargingEffectSystem)), // Write Sprite
    ExecuteAfter(typeof(UpdateShipTravellingEffectSystem)), // Write Sprite
    ExecuteAfter(typeof(UpdateShipTrailEffectSystem)) // Write Sprite
]
// 在颜色同步系统之前执行, 这样子实体也能共享染色
[ExecuteBefore(typeof(SynchronizeColorSystem))] // Write Sprite
public sealed partial class ColorInRangeShipsSystem(World world, IAssetsManager assets)
    : ICalcSystem
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

    public void Update() => SetColorQuery(world);
}

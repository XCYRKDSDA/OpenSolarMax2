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
// 在其他设置外观的系统之后执行以覆写
[
    ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Sprite)),
    ExecuteAfter(
        typeof(ApplyTeamColorSystem),
        "在其他设置外观的系统之后执行以覆写",
        typeof(Sprite)
    ),
    ExecuteAfter(
        typeof(ApplyShipPostBornEffectSystem),
        "在其他设置外观的系统之后执行以覆写",
        typeof(Sprite)
    ),
    ExecuteAfter(
        typeof(UpdateShipChargingEffectSystem),
        "在其他设置外观的系统之后执行以覆写",
        typeof(Sprite)
    ),
    ExecuteAfter(
        typeof(UpdateShipTravellingEffectSystem),
        "在其他设置外观的系统之后执行以覆写",
        typeof(Sprite)
    ),
    ExecuteAfter(
        typeof(UpdateShipTrailEffectSystem),
        "在其他设置外观的系统之后执行以覆写",
        typeof(Sprite)
    ),
    ExecuteBefore(
        typeof(SynchronizeColorSystem),
        "在颜色同步系统之前执行，子实体也能共享染色",
        typeof(Sprite)
    )
]
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

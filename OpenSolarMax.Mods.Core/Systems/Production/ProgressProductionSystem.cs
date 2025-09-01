using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 更新生产系统. 在所有可生产部队的星球上推进生产
/// </summary>
[CoreUpdateSystem]
[ExecuteBefore(typeof(SettleProductionSystem))]
public sealed partial class ProgressProductionSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ProductionAbility, ProductionState, AnchoredShipsRegistry, InParty.AsAffiliate>]
    private static void UpdateProduction([Data] GameTime time, Entity planet, in ProductionAbility ability,
                                         ref ProductionState state)
    {
        state.UnitsProducedThisFrame = 0;

        if (!state.CanProduce)
        {
            // 如果当前星球上无法进行生产, 则归零生产进度
            state.Progress = 0;
            return;
        }

        // 增加生产进度
        state.Progress += ability.ProgressPerSecond * (float)time.ElapsedGameTime.TotalSeconds;

        // 记录生产个数
        ref readonly var asAffiliate = ref planet.Get<InParty.AsAffiliate>();
        ref var producible = ref asAffiliate.Relationship!.Value.Copy.Party.Entity.Get<Producible>();
        while (state.Progress > producible.WorkloadPerShip)
        {
            state.Progress -= producible.WorkloadPerShip;
            state.UnitsProducedThisFrame += 1;
        }
    }
}

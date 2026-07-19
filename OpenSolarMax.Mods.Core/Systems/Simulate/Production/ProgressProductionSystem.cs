// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 更新生产系统. 在所有可生产部队的星球上推进生产
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[
    ReadPrev(typeof(ProductionAbility)),
    ReadPrev(typeof(Producible)),
    ReadPrev(typeof(ProductionCondition)),
    Iterate(typeof(ProductionState))
]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class ProgressProductionSystem(World world) : ITickSystem
{
    [Query]
    [All<ProductionAbility, ProductionState, AnchoredShipsRegistry, InTeam.AsAffiliate>]
    private static void UpdateProduction(
        [Data] GameTime time,
        Entity planet,
        in ProductionAbility ability,
        in ProductionCondition cond,
        ref ProductionState state
    )
    {
        state.ShipsProducedThisFrame = 0;

        if (!cond.IsMet)
        {
            // 如果当前星球上无法进行生产, 则归零生产进度
            state.Progress = 0;
            return;
        }

        // 增加生产进度
        state.Progress += ability.ProgressPerSecond * (float)time.ElapsedGameTime.TotalSeconds;

        // 记录生产个数
        ref readonly var asAffiliate = ref planet.Get<InTeam.AsAffiliate>();
        ref var producible = ref asAffiliate.Relationship!.Value.Copy.Team.Get<Producible>();
        while (state.Progress > producible.WorkloadPerShip)
        {
            state.Progress -= producible.WorkloadPerShip;
            state.ShipsProducedThisFrame += 1;
        }
    }

    public void Update(GameTime gameTime) => UpdateProductionQuery(world, gameTime);
}

#endif

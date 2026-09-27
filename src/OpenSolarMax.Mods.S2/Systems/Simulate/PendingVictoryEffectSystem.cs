using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.Common.Systems.Timing;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, Update]
[Tick(typeof(PendingVictoryEffect))]
public sealed partial class PendingVictoryEffectCountDownSystem(World world)
    : CountDownSystemBase<PendingVictoryEffect>(world) { }

[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(PendingVictoryEffect)),
    ReadCurr(typeof(VictoryEffectTarget)),
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(ReferenceSize)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(Colonizable)),
    Calc(typeof(ColonizationState)),
    DelayedCalc
]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(ColonizationState))]
public sealed partial class FirePendingVictoryEffectSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    [Query]
    [All<PendingVictoryEffect>]
    private void TriggerFire(
        Entity pending,
        in PendingVictoryEffect schedule,
        in VictoryEffectTarget target,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (schedule.TimeLeft > TimeSpan.Zero)
            return;

        var planet = target.Planet;
        var winner = target.Winner;

        // 创建 HaloExplosion 特效
        var transform = planet.Get<AbsoluteTransform>();
        var refSize = planet.Get<ReferenceSize>();

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.HaloExplosion,
            new HaloExplosionDescription
            {
                Team = winner,
                Position = transform.Translation,
                PlanetRadius = refSize.Radius,
            }
        );

        // 创建 ColonizationFlare 特效
        factory.Make(
            world,
            commandBuffer,
            new ColonizationFlareDescription { Planet = planet, Team = winner }
        );

        ref var affiliation = ref planet.Get<InTeam.AsAffiliate>();
        if (affiliation.Relationship is null)
        {
            // 令中立天体立即加入获胜方
            factory.Make(
                world,
                commandBuffer,
                new InTeamDescription { Team = winner, Affiliate = planet }
            );

            // 重置 ColonizationState
            ref var state = ref planet.Get<ColonizationState>();
            state.Team = winner;
            state.Progress = planet.Get<Colonizable>().Volume;
            state.Event = ColonizationEvent.Idle;
        }
        else
        {
            // 如果非中立，只可能是胜利方阵营
            Debug.Assert(affiliation.Relationship.Value.Copy.Team == winner);
        }

        // 销毁 pending 自身
        commandBuffer.Destroy(pending);
    }

    public void Update(CommandBuffer commandBuffer) => TriggerFireQuery(world, commandBuffer);
}

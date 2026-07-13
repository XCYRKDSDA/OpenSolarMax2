using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;
using OpenSolarMax.Mods.Core.Systems.Timing;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
[Iterate(typeof(PendingVictoryEffect))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class PendingVictoryEffectCountDownSystem(World world)
    : CountDownSystemBase<PendingVictoryEffect>(world) { }

[SimulateSystem, BeforeStructuralChanges]
[ReadCurr(typeof(PendingVictoryEffect)), Write(typeof(ColonizationState)), ChangeStructure]
[ExecuteBefore(typeof(ProgressColonizationSystem))]
[ExecuteBefore(typeof(SettleColonizationSystem))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class FirePendingVictoryEffectSystem(World world, IConceptFactory factory)
    : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<PendingVictoryEffect>]
    private void TriggerFire(
        Entity pending,
        in PendingVictoryEffect schedule,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (schedule.TimeLeft > TimeSpan.Zero)
            return;

        var planet = schedule.Planet;
        var winner = schedule.Winner;

        // 创建 HaloExplosion 特效
        var transform = planet.Get<AbsoluteTransform>();
        var refSize = planet.Get<ReferenceSize>();

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.HaloExplosion,
            new HaloExplosionDescription
            {
                Color = winner.Get<TeamReferenceColor>().Value,
                Position = transform.Translation,
                PlanetRadius = refSize.Radius,
            }
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

using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 监测殖民进度，切换或者移除殖民状态，同时播放动画
/// </summary>
[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(ReferenceSize)),
    ReadCurr(typeof(TeamReferenceColor)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(Victory)),
    Consume(typeof(ColonizationState)),
    ChangeStructure
]
public sealed partial class SettleColonizationSystem(
    World world,
    IAssetsManager assets,
    IConceptFactory factory
) : ICalcSystemWithStructuralChanges
{
    private void CreateHaloExplosion(CommandBuffer commandBuffer, Entity planet, Color color)
    {
        ref var planetAbsoluteTransform = ref planet.Get<AbsoluteTransform>();
        ref readonly var refSize = ref planet.Get<ReferenceSize>();
        factory.Make(
            world,
            commandBuffer,
            new HaloExplosionDescription()
            {
                Color = color,
                Position = planetAbsoluteTransform.Translation,
                PlanetRadius = refSize.Radius,
            }
        );
        factory.Make(
            world,
            commandBuffer,
            new ColonizationFlareDescription() { Planet = planet, AfterColor = color }
        );
    }

    [Query]
    [All<InTeam.AsTeam, Victory>]
    private static void CheckAnyTeamWon(ref Victory victory, [Data] ref bool hasWon)
    {
        if (victory.HasWon)
            hasWon = true;
    }

    [Query]
    [All<ColonizationState, InTeam.AsAffiliate>]
    private void SettleColonization(
        Entity planet,
        ref ColonizationState state,
        in InTeam.AsAffiliate asTeamAffiliate,
        [Data] bool hasWon,
        [Data] CommandBuffer commandBuffer
    )
    {
        var planetTeam = asTeamAffiliate.Relationship?.Copy.Team;

        if (state.Event == ColonizationEvent.Finished)
        {
            // 胜利已判定时不播放殖民完成特效，归属变更照常执行
            if (!hasWon)
                CreateHaloExplosion(
                    commandBuffer,
                    planet,
                    state.Team.Get<TeamReferenceColor>().Value
                );

            // 完成殖民
            if (planetTeam is null)
            {
                factory.Make(
                    world,
                    commandBuffer,
                    new InTeamDescription() { Team = state.Team, Affiliate = planet }
                );
            }
        }
        else if (state.Event == ColonizationEvent.Destroyed)
        {
            // 胜利已判定时不播放解除特效，归属变更照常执行
            if (!hasWon)
                CreateHaloExplosion(commandBuffer, planet, Color.White);

            // 解除当前阵营的殖民
            if (planetTeam is not null)
                commandBuffer.Destroy(asTeamAffiliate.Relationship!.Value.Ref);
        }

        state.Event = ColonizationEvent.Idle;
    }

    public void Update(CommandBuffer commandBuffer)
    {
        var hasWon = false;
        CheckAnyTeamWonQuery(world, ref hasWon);
        SettleColonizationQuery(world, hasWon, commandBuffer);
    }
}

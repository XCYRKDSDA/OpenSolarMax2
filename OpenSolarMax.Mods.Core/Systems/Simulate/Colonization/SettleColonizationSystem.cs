// 整文件禁用：ECS 框架层重构后待迁移
#if false
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
[SimulateSystem, BeforeStructuralChanges]
[
    ReadPrev(typeof(AbsoluteTransform)),
    ReadPrev(typeof(ReferenceSize)),
    ReadPrev(typeof(TeamReferenceColor)),
    ReadPrev(typeof(InTeam.AsAffiliate)),
    Iterate(typeof(ColonizationState)),
    ChangeStructure
]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
// 先计算进度，再判断是否完成殖民
[ExecuteAfter(typeof(ProgressColonizationSystem))]
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
    }

    [Query]
    [All<ColonizationState, InTeam.AsAffiliate>]
    private void SettleColonization(
        Entity planet,
        ref ColonizationState state,
        in InTeam.AsAffiliate asTeamAffiliate,
        [Data] CommandBuffer commandBuffer
    )
    {
        var planetTeam = asTeamAffiliate.Relationship?.Copy.Team;

        if (state.Event == ColonizationEvent.Finished)
        {
            // 不管怎样，先开香槟
            CreateHaloExplosion(commandBuffer, planet, state.Team.Get<TeamReferenceColor>().Value);

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
            // 开香槟
            CreateHaloExplosion(commandBuffer, planet, Color.White);

            // 解除当前阵营的殖民
            if (planetTeam is not null)
                commandBuffer.Destroy(asTeamAffiliate.Relationship!.Value.Ref);
        }
    }

    public void Update(CommandBuffer commandBuffer) =>
        SettleColonizationQuery(world, commandBuffer);
}

#endif

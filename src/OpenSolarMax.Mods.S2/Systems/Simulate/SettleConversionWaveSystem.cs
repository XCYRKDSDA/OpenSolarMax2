using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 把转化波圆内的天体与舰船转为目标阵营，并同步视觉特效的尺寸
/// </summary>
[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(ConversionWaveConfig)),
    ReadCurr(typeof(ConversionWaveState)),
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(Colonizable)),
    ReadCurr(typeof(ReferenceSize)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(PopulationCost)),
    Calc(typeof(Sprite)),
    Calc(typeof(ColonizationState)),
    DelayedCalc
]
[ExecuteAfter(
    typeof(ApplyAnimationSystem),
    "默认动画系统优先执行",
    typeof(Sprite),
    typeof(ColonizationState)
)]
[ExecuteAfter(
    typeof(FirePendingVictoryEffectSystem),
    "转化波结算始终覆盖胜利归属，须在其后执行",
    typeof(ColonizationState)
)]
[
    FineWith(
        typeof(SynchronizeColorSystem),
        "本系统只缩放波的视觉精灵，不设置颜色",
        typeof(Sprite)
    ),
    FineWith(
        typeof(ApplyVisualStyleSystem),
        "本系统只缩放波的视觉精灵，不设置颜色与混合模式",
        typeof(Sprite)
    ),
    FineWith(
        typeof(UpdateShipChargingEffectSystem),
        "本系统只缩放波的视觉精灵，与舰船动画互不冲突",
        typeof(Sprite)
    ),
    FineWith(
        typeof(UpdateShipTravellingEffectSystem),
        "本系统只缩放波的视觉精灵，与舰船动画互不冲突",
        typeof(Sprite)
    ),
    FineWith(
        typeof(UpdateShipTrailEffectSystem),
        "本系统只缩放波的视觉精灵，与舰船动画互不冲突",
        typeof(Sprite)
    ),
    FineWith(
        typeof(ApplyShipsWarpingEffectSystem),
        "本系统只缩放波的视觉精灵，与舰船动画互不冲突",
        typeof(Sprite)
    ),
    FineWith(
        typeof(ApplyShipPostBornEffectSystem),
        "本系统只缩放波的视觉精灵，与舰船动画互不冲突",
        typeof(Sprite)
    )
]
public sealed partial class SettleConversionWaveSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    private void ConvertTeam(
        Entity entity,
        in InTeam.AsAffiliate asAffiliate,
        Entity conversionTeam,
        CommandBuffer commandBuffer
    )
    {
        if (asAffiliate.Relationship is not null)
            commandBuffer.Destroy(asAffiliate.Relationship.Value.Ref);

        factory.Make(
            world,
            commandBuffer,
            new InTeamDescription { Team = conversionTeam, Affiliate = entity }
        );
    }

    [Query]
    [All<Colonizable, ReferenceSize, InTeam.AsAffiliate, AbsoluteTransform>]
    private void ConvertCelestialBody(
        Entity celestialBody,
        in Colonizable colonizable,
        in ReferenceSize referenceSize,
        in InTeam.AsAffiliate asAffiliate,
        in AbsoluteTransform pose,
        [Data] in AbsoluteTransform center,
        [Data] in float radius,
        [Data] Entity conversionTeam,
        [Data] CommandBuffer commandBuffer
    )
    {
        var diffX = pose.Translation.X - center.Translation.X;
        var diffY = pose.Translation.Y - center.Translation.Y;
        if (diffX * diffX + diffY * diffY > radius * radius)
            return;

        if (asAffiliate.Relationship?.Copy.Team != conversionTeam)
        {
            ConvertTeam(celestialBody, in asAffiliate, conversionTeam, commandBuffer);

            // 转化演出：光环爆炸与殖民闪光，做法同胜利归属结算
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.HaloExplosion,
                new HaloExplosionDescription
                {
                    Team = conversionTeam,
                    Position = pose.Translation,
                    PlanetRadius = referenceSize.Radius,
                }
            );

            factory.Make(
                world,
                commandBuffer,
                new ColonizationFlareDescription { Planet = celestialBody, Team = conversionTeam }
            );
        }

        // 占领度直接改写为目标阵营的满进度。直接写入先行落地，殖民结算随后读到的便是
        // Idle，不再触发归属变更与特效
        ref var state = ref celestialBody.Get<ColonizationState>();
        if (
            state.Team != conversionTeam
            || state.Progress != colonizable.Volume
            || state.Event != ColonizationEvent.Idle
        )
        {
            state.Team = conversionTeam;
            state.Progress = colonizable.Volume;
            state.Event = ColonizationEvent.Idle;
        }
    }

    [Query]
    [All<PopulationCost, InTeam.AsAffiliate, AbsoluteTransform>]
    private void ConvertShip(
        Entity ship,
        in InTeam.AsAffiliate asAffiliate,
        in AbsoluteTransform pose,
        [Data] in AbsoluteTransform center,
        [Data] in float radius,
        [Data] Entity conversionTeam,
        [Data] CommandBuffer commandBuffer
    )
    {
        var diffX = pose.Translation.X - center.Translation.X;
        var diffY = pose.Translation.Y - center.Translation.Y;
        if (diffX * diffX + diffY * diffY > radius * radius)
            return;

        if (asAffiliate.Relationship?.Copy.Team == conversionTeam)
            return;

        ConvertTeam(ship, in asAffiliate, conversionTeam, commandBuffer);
    }

    [Query]
    [All<ConversionWaveConfig, ConversionWaveState, AbsoluteTransform, Sprite>]
    private void Settle(
        in ConversionWaveConfig config,
        in ConversionWaveState state,
        in AbsoluteTransform pose,
        ref Sprite sprite,
        [Data] CommandBuffer commandBuffer
    )
    {
        ConvertCelestialBodyQuery(
            world,
            in pose,
            in state.Radius,
            config.ConversionTeam,
            commandBuffer
        );
        ConvertShipQuery(world, in pose, in state.Radius, config.ConversionTeam, commandBuffer);

        // 同步视觉特效尺寸
        sprite.Size = new Vector2(state.Radius * 2);
    }

    public void Update(CommandBuffer commandBuffer) => SettleQuery(world, commandBuffer);
}

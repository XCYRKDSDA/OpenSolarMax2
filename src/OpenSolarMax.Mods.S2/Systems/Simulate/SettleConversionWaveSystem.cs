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
/// 结算转化波：把圆内的天体与舰船转为目标阵营，并把视觉子实体的尺寸写为当前直径；
/// 半径超出关卡范围后销毁转化波
/// </summary>
[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(ConversionWaveConfig)),
    ReadCurr(typeof(ConversionWaveState)),
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(Colonizable)),
    ReadCurr(typeof(ReferenceSize)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(Camera)),
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
    [All<Camera, AbsoluteTransform>]
    private static void MeasureLevelBound(
        in Camera camera,
        in AbsoluteTransform viewPose,
        [Data] in Vector3 center,
        [Data] ref float bound
    )
    {
        // 关卡范围为相机矩形，取其四角到波心的最远距离作为销毁阈值
        var halfWidth = camera.Width * 0.5f;
        var halfHeight = camera.Height * 0.5f;

        var diffXToLeft = MathF.Abs(center.X - (viewPose.Translation.X - halfWidth));
        var diffXToRight = MathF.Abs(center.X - (viewPose.Translation.X + halfWidth));
        var diffYToBottom = MathF.Abs(center.Y - (viewPose.Translation.Y - halfHeight));
        var diffYToTop = MathF.Abs(center.Y - (viewPose.Translation.Y + halfHeight));

        var maxDiffX = MathF.Max(diffXToLeft, diffXToRight);
        var maxDiffY = MathF.Max(diffYToBottom, diffYToTop);

        bound = MathF.Max(bound, MathF.Sqrt(maxDiffX * maxDiffX + maxDiffY * maxDiffY));
    }

    [Query]
    [All<ConversionWaveConfig, ConversionWaveState, AbsoluteTransform>]
    private void Settle(
        Entity wave,
        in ConversionWaveConfig config,
        in ConversionWaveState state,
        in AbsoluteTransform pose,
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

        // 视觉子实体的尺寸写为当前直径的两倍
        if (config.Visual.IsAlive())
            config.Visual.Get<Sprite>().Size = new Vector2(state.Radius * 4);

        var bound = 0f;
        MeasureLevelBoundQuery(world, in pose.Translation, ref bound);
        if (bound > 0 && state.Radius > bound)
            commandBuffer.Destroy(wave);
    }

    public void Update(CommandBuffer commandBuffer) => SettleQuery(world, commandBuffer);
}

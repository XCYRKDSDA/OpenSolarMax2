using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 根据舰船跳跃任务执行的进度，应用尾迹的拉伸与熄灭动画
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(TrailOf.AsTrail))]
[ReadCurr(typeof(JumpingStatus))]
[Write(typeof(Sprite))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[FineWith(typeof(SynchronizeColorSystem))] // Write Sprite
[FineWith(typeof(ApplyTeamColorSystem))] // Write Sprite
[FineWith(typeof(UpdateShipChargingEffectSystem))] // Write Sprite
[FineWith(typeof(UpdateShipTravellingEffectSystem))] // Write Sprite
public sealed partial class UpdateShipTrailEffectSystem(
    World world,
    IAssetsManager assets,
    [Section("systems:simulate:jumping")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _landingDuration = configs.RequireValue<float>("landing_duration");

    private readonly AnimationClip<Entity> _trailStretchingAnimation = assets.Load<
        AnimationClip<Entity>
    >("Animations/TrailStretching.json");

    private readonly AnimationClip<Entity> _trailExtinguishedAnimation = assets.Load<
        AnimationClip<Entity>
    >("Animations/TrailExtinguished.json");

    [Query]
    [All<TrailOf.AsTrail, Sprite>]
    private void UpdateTrailEffect(Entity trail, in TrailOf.AsTrail asTrail)
    {
        // 创建 Trail 的同时一定会创建 AsTrail 关系
        Debug.Assert(asTrail.Relationship is not null);

        // 从关系副本反向取得舰船
        var ship = asTrail.Relationship.Value.Copy.Ship;
        var status = ship.Get<JumpingStatus>();

        // Trail 只会在舰船为 Travelling 状态时存在
        Debug.Assert(status.State == JumpingState.Travelling);

        // 应用尾迹动画
        if (
            status.Travelling.ElapsedTime + status.Travelling.DelayedTime
            < status.Task.ExpectedTravelDuration - _landingDuration
        )
        {
            var stretchingAnimationTime = status.Travelling.ElapsedTime;
            AnimationEvaluator<Entity>.EvaluateAndSet(
                ref trail,
                _trailStretchingAnimation,
                stretchingAnimationTime
            );
        }
        else
        {
            var stretchingAnimationTime = status.Travelling.ElapsedTime;
            var crossTime =
                status.Travelling.ElapsedTime
                + status.Travelling.DelayedTime
                - (status.Task.ExpectedTravelDuration - _landingDuration);
            var crossRatio = crossTime / _landingDuration;

            // 此处不是淡出，而只是单纯地用多个动画的交融构造效果
            AnimationEvaluator<Entity>.TweenAndSet(
                ref trail,
                _trailStretchingAnimation,
                stretchingAnimationTime,
                _trailExtinguishedAnimation,
                crossTime,
                null,
                crossRatio
            ); // 采用默认的线性差值
        }
    }

    public void Update() => UpdateTrailEffectQuery(world);
}

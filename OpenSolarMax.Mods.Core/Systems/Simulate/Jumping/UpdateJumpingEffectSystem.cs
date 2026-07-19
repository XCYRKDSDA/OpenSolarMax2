// 整文件禁用：ECS 框架层重构后待迁移
#if false
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
/// 根据跳跃任务执行的时间和阶段，应用舰船及其尾焰的动画
/// </summary>
[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TrailOf.AsShip)), ReadCurr(typeof(JumpingStatus)), Write(typeof(Sprite))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[FineWith(typeof(ApplyTeamColorSystem))] // 该系统只改尾迹的颜色，尾迹不会与阵营直接挂钩
[ExecuteAfter(typeof(ApplyShipPostBornEffectSystem))] // 如果一个舰船刚出生就移动，则用移动动画覆盖其出生动画
[ExecuteBefore(typeof(SynchronizeColorSystem))]
public sealed partial class UpdateJumpingEffectSystem(
    World world,
    IAssetsManager assets,
    [Section("systems:simulate:jumping")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _landingDuration = configs.RequireValue<float>("landing_duration");

    private readonly float _shipJumpingFadeInDuration = configs.RequireValue<float>(
        "fading_in_duration"
    );
    private readonly float _shipJumpingFadeOutDuration = configs.RequireValue<float>(
        "fading_out_duration"
    );

    private readonly AnimationClip<Entity> _shipJumpingAnimationClip = assets.Load<
        AnimationClip<Entity>
    >("Animations/ShipJumping.json");

    private readonly AnimationClip<Entity> _shipTakingOffAnimationClip = assets.Load<
        AnimationClip<Entity>
    >("Animations/ShipTakingOff.json");

    private readonly AnimationClip<Entity> _trailStretchingAnimation = assets.Load<
        AnimationClip<Entity>
    >("Animations/TrailStretching.json");

    private readonly AnimationClip<Entity> _trailExtinguishedAnimation = assets.Load<
        AnimationClip<Entity>
    >("Animations/TrailExtinguished.json");

    [Query]
    [All<TrailOf.AsShip, JumpingStatus, Sprite, Animation>]
    private void CalculateAnimation(
        Entity ship,
        in TrailOf.AsShip asShip,
        in JumpingStatus status,
        in Sprite sprite
    )
    {
        if (status.State == JumpingState.Idle)
            return;

        // Charging状态下播放起飞动画
        if (status.State == JumpingState.Charging)
        {
            var takingOffAnimationTime = status.Charging.ElapsedTime;
            var fadeInTime = status.Charging.ElapsedTime;
            var fadeInRatio = fadeInTime / _shipJumpingFadeInDuration;

            switch (fadeInRatio)
            {
                case >= 0 and < 1:
                    AnimationEvaluator<Entity>.TweenAndSet(
                        ref ship,
                        null,
                        float.NaN, // 上一个动画设置为空，直接继承上一个系统设置的值
                        _shipTakingOffAnimationClip,
                        takingOffAnimationTime,
                        null,
                        fadeInRatio
                    ); // 采用默认的线性差值
                    break;
                case >= 1:
                    AnimationEvaluator<Entity>.EvaluateAndSet(
                        ref ship,
                        _shipTakingOffAnimationClip,
                        takingOffAnimationTime
                    );
                    break;
            }
        }
        // Travelling状态下播放飞行动画
        else if (status.State == JumpingState.Travelling)
        {
            var jumpingAnimationTime = status.Travelling.ElapsedTime;
            var fadeOutTime =
                status.Travelling.ElapsedTime
                + status.Travelling.DelayedTime
                - (status.Task.ExpectedTravelDuration - _shipJumpingFadeOutDuration);
            var fadeOutRatio = fadeOutTime / _shipJumpingFadeOutDuration;

            switch (fadeOutRatio)
            {
                case < 0:
                    AnimationEvaluator<Entity>.EvaluateAndSet(
                        ref ship,
                        _shipJumpingAnimationClip,
                        jumpingAnimationTime
                    );
                    break;
                case >= 0 and < 1:
                    AnimationEvaluator<Entity>.TweenAndSet(
                        ref ship,
                        _shipJumpingAnimationClip,
                        jumpingAnimationTime,
                        null,
                        float.NaN, // 下一个动画设置为空，直接继承上一个系统设置的值
                        null,
                        fadeOutRatio
                    ); // 采用默认的线性差值
                    break;
            }
        }

        // 处理尾迹效果
        var trail = asShip.Relationship!.Value.Copy.Trail;

        // 尾迹的颜色和舰船的颜色相同
        Debug.Assert(trail.Has<Sprite>());
        trail.Get<Sprite>().Color = sprite.Color;

        // 应用尾迹动画
        if (status.State == JumpingState.Travelling)
        {
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
    }

    public void Update() => CalculateAnimationQuery(world);
}

#endif

using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 根据传送任务执行的进度，应用舰船传送前后的朝向与动画效果
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(WarpingStatus))]
[Write(typeof(AbsoluteTransform))]
[Write(typeof(Sprite))]
[ExecuteAfter(
    typeof(ApplyAnimationSystem),
    "默认动画系统优先执行",
    typeof(Sprite),
    typeof(AbsoluteTransform)
)]
[ExecuteAfter(
    typeof(CalculateAbsoluteTransformSystem),
    "在自动计算绝对位姿系统之后以覆盖位姿",
    typeof(AbsoluteTransform)
)]
[ExecuteAfter(typeof(ApplyShipPostBornEffectSystem), "覆盖新生舰船动画", typeof(Sprite))]
[FineWith(typeof(CalculateShipPositionSystem), "跃迁和飞行完全不相干", typeof(AbsoluteTransform))]
[FineWith(typeof(StartJumpingSystem), "跃迁和飞行完全不相干", typeof(AbsoluteTransform))]
[FineWith(typeof(UpdateShipChargingEffectSystem), "跃迁和飞行完全不相干", typeof(Sprite))]
[FineWith(typeof(UpdateShipTrailEffectSystem), "跃迁和飞行完全不相干", typeof(Sprite))]
[FineWith(typeof(UpdateShipTravellingEffectSystem), "跃迁和飞行完全不相干", typeof(Sprite))]
[FineWith(typeof(ApplyTeamColorSystem), "本系统不设置颜色，无冲突", typeof(Sprite))]
[FineWith(typeof(SynchronizeColorSystem), "本系统不设置颜色，无冲突", typeof(Sprite))]
public sealed partial class ApplyShipsWarpingEffectSystem(World world, IAssetsManager assets)
    : ICalcSystem
{
    private readonly AnimationClip<Entity> _shipPreWarpAnimationClip = assets.Load<
        AnimationClip<Entity>
    >("Animations/ShipPreWarp.json");

    private readonly AnimationClip<Entity> _shipPostWarpAnimationClip = assets.Load<
        AnimationClip<Entity>
    >("Animations/ShipPostWarp.json");

    [Query]
    [All<WarpingStatus, Sprite, AbsoluteTransform>]
    private void ApplyEffect(Entity ship, in WarpingStatus status, ref AbsoluteTransform pose)
    {
        if (status.State == WarpingState.PreWarp)
        {
            // 面向目标位置
            var head = ship.Get<AbsoluteTransform>().Translation;

            var destinationPlanetPose = status
                .Task.DestinationPlanet.Get<AbsoluteTransform>()
                .TransformToRoot;
            var expectedPoseInDestination = RevolutionUtils
                .CalculateTransform(
                    status.Task.ExpectedRevolutionOrbit,
                    status.Task.ExpectedRevolutionState
                )
                .TransformToParent;
            var tail = (expectedPoseInDestination * destinationPlanetPose).Translation;

            pose.Rotation = TransformProjection.UprightAim(tail - head);

            // 播放动画
            var animationTime = (float)status.PreWarp.ElapsedTime.TotalSeconds;

            if (animationTime < 0.25f) // 用0.5秒渐入
                AnimationEvaluator<Entity>.TweenAndSet(
                    ref ship,
                    null,
                    float.NaN,
                    _shipPreWarpAnimationClip,
                    animationTime,
                    null,
                    animationTime / 0.25f
                );
            else
                AnimationEvaluator<Entity>.EvaluateAndSet(
                    ref ship,
                    _shipPreWarpAnimationClip,
                    animationTime
                );
        }
        else if (status.State == WarpingState.PostWarp)
        {
            // 播放动画
            var animationTime = (float)status.PostWarp.ElapsedTime.TotalSeconds;

            AnimationEvaluator<Entity>.TweenAndSet(
                ref ship,
                _shipPostWarpAnimationClip,
                animationTime,
                null,
                float.NaN,
                null,
                animationTime
            );
        }
    }

    public void Update() => ApplyEffectQuery(world);
}

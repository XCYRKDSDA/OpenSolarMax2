using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 根据运输任务执行的时间和阶段，应用单位及其尾焰的动画
/// </summary>
[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TrailOf.AsShip), withEntities: true), ReadCurr(typeof(ShippingStatus))]
[Write(typeof(Sprite))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[FineWith(typeof(ApplyPartyColorSystem))] // 该系统只改尾迹的颜色，尾迹不会与阵营直接挂钩
[ExecuteAfter(typeof(ApplyUnitPostBornEffectSystem))] // 如果一个单位刚出生就移动，则用移动动画覆盖其出生动画
public sealed partial class UpdateShippingEffectSystem(World world, IAssetsManager assets) : ICalcSystem
{
    private const float _landDuration = 0.5f;

    private const float _unitShippingFadeInDuration = 0.1f;
    private const float _unitShippingFadeOutDuration = _landDuration / 2;

    private readonly AnimationClip<Entity> _unitShippingAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitShipping.json");

    private readonly AnimationClip<Entity> _unitTakingOffAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitTakingOff.json");

    private readonly AnimationClip<Entity> _trailStretchingAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/TrailStretching.json");

    private readonly AnimationClip<Entity> _trailExtinguishedAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/TrailExtinguished.json");

    [Query]
    [All<TrailOf.AsShip, ShippingStatus, Sprite, Animation>]
    private void CalculateAnimation(Entity ship, in TrailOf.AsShip asShip, in ShippingStatus status, in Sprite sprite)
    {
        if (status.State == ShippingState.Idle)
            return;

        // Charging状态下播放起飞动画
        if (status.State == ShippingState.Charging)
        {
            var takingOffAnimationTime = status.Charging.ElapsedTime;
            var fadeInTime = status.Charging.ElapsedTime;
            var fadeInRatio = fadeInTime / _unitShippingFadeInDuration;

            switch (fadeInRatio)
            {
                case >= 0 and < 1:
                    AnimationEvaluator<Entity>.TweenAndSet(ref ship,
                                                           null, float.NaN, // 上一个动画设置为空，直接继承上一个系统设置的值
                                                           _unitTakingOffAnimationClip, takingOffAnimationTime,
                                                           null, fadeInRatio); // 采用默认的线性差值
                    break;
                case >= 1:
                    AnimationEvaluator<Entity>.EvaluateAndSet(ref ship, _unitTakingOffAnimationClip,
                                                              takingOffAnimationTime);
                    break;
            }
        }
        // Travelling状态下播放飞行动画
        else if (status.State == ShippingState.Travelling)
        {
            var shippingAnimationTime = status.Travelling.ElapsedTime;
            var fadeOutTime = status.Travelling.ElapsedTime + status.Travelling.DelayedTime -
                              (status.Task.ExpectedTravelDuration - _unitShippingFadeOutDuration);
            var fadeOutRatio = fadeOutTime / _unitShippingFadeOutDuration;

            switch (fadeOutRatio)
            {
                case < 0:
                    AnimationEvaluator<Entity>.EvaluateAndSet(ref ship, _unitShippingAnimationClip,
                                                              shippingAnimationTime);
                    break;
                case >= 0 and < 1:
                    AnimationEvaluator<Entity>.TweenAndSet(ref ship,
                                                           _unitShippingAnimationClip, shippingAnimationTime,
                                                           null, float.NaN, // 下一个动画设置为空，直接继承上一个系统设置的值
                                                           null, fadeOutRatio); // 采用默认的线性差值
                    break;
            }
        }

        // 处理尾迹效果
        var trail = asShip.Relationship!.Value.Copy.Trail;

        // 尾迹的颜色和单位的颜色相同
        Debug.Assert(trail.Has<Sprite>());
        trail.Get<Sprite>().Color = sprite.Color;

        // 应用尾迹动画
        if (status.State == ShippingState.Travelling)
        {
            if (status.Travelling.ElapsedTime + status.Travelling.DelayedTime
                < status.Task.ExpectedTravelDuration - _landDuration)
            {
                var stretchingAnimationTime = status.Travelling.ElapsedTime;
                AnimationEvaluator<Entity>.EvaluateAndSet(ref trail, _trailStretchingAnimation,
                                                          stretchingAnimationTime);
            }
            else
            {
                var stretchingAnimationTime = status.Travelling.ElapsedTime;
                var crossTime = status.Travelling.ElapsedTime + status.Travelling.DelayedTime -
                                (status.Task.ExpectedTravelDuration - _landDuration);
                var crossRatio = crossTime / _landDuration;

                // 此处不是淡出，而只是单纯地用多个动画的交融构造效果
                AnimationEvaluator<Entity>.TweenAndSet(ref trail,
                                                       _trailStretchingAnimation, stretchingAnimationTime,
                                                       _trailExtinguishedAnimation, crossTime,
                                                       null, crossRatio); // 采用默认的线性差值
            }
        }
    }

    public void Update() => CalculateAnimationQuery(world);
}

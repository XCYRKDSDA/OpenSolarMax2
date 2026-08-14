using Arch.Core;
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
/// 根据跳跃任务的充能阶段，应用舰船的起飞动画
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(JumpingStatus))]
[Write(typeof(Sprite))]
[
    ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Sprite)),
    FineWith(typeof(SynchronizeColorSystem), "本系统不设置颜色，无冲突", typeof(Sprite)),
    FineWith(typeof(ApplyTeamColorSystem), "本系统不设置颜色，无冲突", typeof(Sprite)),
    FineWith(typeof(UpdateShipTravellingEffectSystem), "飞行状态是互斥的", typeof(Sprite)),
    FineWith(typeof(UpdateShipTrailEffectSystem), "飞船和尾迹是不同实体，无冲突", typeof(Sprite))
]
public sealed partial class UpdateShipChargingEffectSystem(
    World world,
    IAssetsManager assets,
    [Section("systems:simulate:jumping")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _shipJumpingFadeInDuration = configs.RequireValue<float>(
        "fading_in_duration"
    );

    private readonly AnimationClip<Entity> _shipTakingOffAnimationClip = assets.Load<
        AnimationClip<Entity>
    >("Animations/ShipTakingOff.json");

    [Query]
    [All<JumpingStatus>]
    private void CalculateAnimation(Entity ship, in JumpingStatus status)
    {
        if (status.State != JumpingState.Charging)
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
    }

    public void Update() => CalculateAnimationQuery(world);
}

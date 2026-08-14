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
/// 根据跳跃任务执行的时间，应用舰船 Travelling 状态下的飞行及淡出动画
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(JumpingStatus))]
[Write(typeof(Sprite))]
[
    ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(Sprite)),
    FineWith(typeof(SynchronizeColorSystem), "本系统不设置颜色，无冲突", typeof(Sprite)),
    FineWith(typeof(ApplyTeamColorSystem), "本系统不设置颜色，无冲突", typeof(Sprite)),
    FineWith(typeof(UpdateShipChargingEffectSystem), "飞行状态是互斥的", typeof(Sprite)),
    FineWith(typeof(UpdateShipTrailEffectSystem), "飞船和尾迹是不同实体，无冲突 ", typeof(Sprite))
]
public sealed partial class UpdateShipTravellingEffectSystem(
    World world,
    IAssetsManager assets,
    [Section("systems:simulate:jumping")] IConfiguration configs
) : ICalcSystem
{
    private readonly float _shipJumpingFadeOutDuration = configs.RequireValue<float>(
        "fading_out_duration"
    );

    private readonly AnimationClip<Entity> _shipJumpingAnimationClip = assets.Load<
        AnimationClip<Entity>
    >("Animations/ShipJumping.json");

    [Query]
    [All<JumpingStatus>]
    private void CalculateTravellingAnimation(Entity ship, in JumpingStatus status)
    {
        if (status.State != JumpingState.Travelling)
            return;

        // Travelling状态下播放飞行动画
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

    public void Update() => CalculateTravellingAnimationQuery(world);
}

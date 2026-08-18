using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using FMOD.Studio;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 销毁动画与音效均已完成的传送门充能特效实体
/// </summary>
/// <remarks>
/// assignment 实体自身没有 Animation 组件——动画在 flare 子实体上
/// （BackFlare / SurroundFlares 各持 Animation），这里经 Entity.Get 动态读取
/// </remarks>
[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(WarpChargingEffectAssignment)),
    ReadCurr(typeof(SoundEffect)),
    ReadCurr(typeof(Animation)),
    ChangeStructure
]
public sealed partial class DestroyFinishedWarpChargingEffectsSystem(World world)
    : ICalcSystemWithStructuralChanges
{
    private static bool AnimationDone(in Animation animation)
    {
        // 原始动画剪辑尚未烘焙（首帧），跳过本实体暂不判定
        if (animation.RawClip is not null)
            return false;

        // 没有指定动画剪辑也算是播完了
        return (
            animation.Clip is null
            || (animation.TimeElapsed + animation.TimeOffset).TotalSeconds > animation.Clip.Length
        );
    }

    [Query]
    [All<WarpChargingEffectAssignment>]
    private static void ExpireEffects(
        [Data] CommandBuffer commands,
        Entity entity,
        in WarpChargingEffectAssignment assignment,
        in SoundEffect soundEffect
    )
    {
        // 全部光晕动画完成（含 TimeOffset 延迟修正），音效停止后才销毁
        bool animationDone =
            AnimationDone(in assignment.BackFlare.Get<Animation>())
            && assignment.SurroundFlares.All(r => AnimationDone(in r.Get<Animation>()));

        soundEffect.EventInstance.getPlaybackState(out var playbackState);
        bool soundEffectDone = playbackState == PLAYBACK_STATE.STOPPED;

        if (animationDone && soundEffectDone)
        {
            // 与 ManageDependenceSystem 的级联销毁可能重复触发本销毁，Destroy 幂等，重复安全
            commands.Destroy(entity);
        }
    }

    public void Update(CommandBuffer commandBuffer) => ExpireEffectsQuery(world, commandBuffer);
}

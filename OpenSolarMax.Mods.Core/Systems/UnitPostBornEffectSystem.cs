using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

using Params = UnitPostBornEffectParams;

internal static class UnitPostBornEffectParams
{
    internal static readonly TimeSpan PostBornDuration = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan FadeOutDuration = PostBornDuration * 0.1;
}

[CoreUpdateSystem]
public partial class UpdateUnitPostBornEffectSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<UnitPostBornEffect>]
    private static void UpdateBlinkEffect(ref UnitPostBornEffect effect, [Data] GameTime time)
    {
        effect.TimeElapsed += time.ElapsedGameTime;
    }
}

[StructuralChangeSystem]
public partial class RemoveUnitPostBornEffectSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<UnitPostBornEffect>]
    private void RemoveUnitPostBornEffect(Entity entity, in UnitPostBornEffect effect)
    {
        if (effect.TimeElapsed >= Params.PostBornDuration)
            _commandBuffer.Remove<UnitPostBornEffect>(entity);
    }

    public override void Update(in GameTime time)
    {
        RemoveUnitPostBornEffectQuery(World);
        _commandBuffer.Playback(World);
    }
}

[LateUpdateSystem]
public partial class ApplyUnitPostBornEffectSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    /// <summary>
    /// 外置的单位出生后动画。<br/>
    /// 要求的组件为<see cref="Sprite"/>
    /// </summary>
    private readonly AnimationClip<Entity> _unitPostBornAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitPostBorn.json");

    [Query]
    [All<UnitPostBornEffect, Sprite>]
    private void ApplyBlinkEffect(Entity entity, in UnitPostBornEffect effect)
    {
        var animationTime = (float)effect.TimeElapsed.TotalSeconds;
        var fadeOutTime = effect.TimeElapsed - (Params.PostBornDuration - Params.FadeOutDuration);
        var fadeOutRatio = (float)(fadeOutTime / Params.FadeOutDuration);
        switch (fadeOutRatio)
        {
            case < 0:
                AnimationEvaluator<Entity>.EvaluateAndSet(ref entity, _unitPostBornAnimationClip, animationTime);
                break;
            case >= 0 and < 1:
                AnimationEvaluator<Entity>.TweenAndSet(ref entity,
                                                       _unitPostBornAnimationClip, animationTime,
                                                       null, float.NaN,
                                                       null, fadeOutRatio);
                break;
        }
    }
}

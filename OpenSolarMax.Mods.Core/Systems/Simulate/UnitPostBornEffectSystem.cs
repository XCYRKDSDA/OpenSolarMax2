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

[SimulateSystem, BeforeStructuralChanges, Iterate(typeof(UnitPostBornEffect))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public partial class UpdateUnitPostBornEffectSystem(World world) : ITickSystem
{
    [Query]
    [All<UnitPostBornEffect>]
    private static void UpdateBlinkEffect(ref UnitPostBornEffect effect, [Data] GameTime time)
    {
        effect.TimeElapsed += time.ElapsedGameTime;
    }

    public void Update(GameTime gameTime) => UpdateBlinkEffectQuery(world, gameTime);
}

[SimulateSystem, BeforeStructuralChanges, ReadCurr(typeof(UnitPostBornEffect)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public partial class RemoveUnitPostBornEffectSystem(World world) : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<UnitPostBornEffect>]
    private static void RemoveUnitPostBornEffect(Entity entity, in UnitPostBornEffect effect,
                                                 [Data] CommandBuffer commandBuffer)
    {
        if (effect.TimeElapsed >= Params.PostBornDuration)
            commandBuffer.Remove<UnitPostBornEffect>(entity);
    }

    public void Update(CommandBuffer commandBuffer) => RemoveUnitPostBornEffectQuery(world, commandBuffer);
}

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(UnitPostBornEffect)), Write(typeof(Sprite))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[FineWith(typeof(ApplyPartyColorSystem))] // 当前系统仅设置透明度和缩放，和应用颜色不冲突
public partial class ApplyUnitPostBornEffectSystem(World world, IAssetsManager assets) : ICalcSystem
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

    public void Update() => ApplyBlinkEffectQuery(world);
}

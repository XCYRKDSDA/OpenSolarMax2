// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

using Params = ShipPostBornEffectParams;

internal static class ShipPostBornEffectParams
{
    internal static readonly TimeSpan PostBornDuration = TimeSpan.FromSeconds(1);
    internal static readonly TimeSpan FadeOutDuration = PostBornDuration * 0.1;
}

[SimulateSystem, BeforeStructuralChanges]
[Iterate(typeof(ShipPostBornEffect))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public partial class UpdateShipPostBornEffectSystem(World world) : ITickSystem
{
    [Query]
    [All<ShipPostBornEffect>]
    private static void UpdateBlinkEffect(ref ShipPostBornEffect effect, [Data] GameTime time)
    {
        effect.TimeElapsed += time.ElapsedGameTime;
    }

    public void Update(GameTime gameTime) => UpdateBlinkEffectQuery(world, gameTime);
}

[SimulateSystem, BeforeStructuralChanges]
[ReadCurr(typeof(ShipPostBornEffect)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public partial class RemoveShipPostBornEffectSystem(World world) : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<ShipPostBornEffect>]
    private static void RemoveShipPostBornEffect(
        Entity entity,
        in ShipPostBornEffect effect,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (effect.TimeElapsed >= Params.PostBornDuration)
            commandBuffer.Remove<ShipPostBornEffect>(entity);
    }

    public void Update(CommandBuffer commandBuffer) =>
        RemoveShipPostBornEffectQuery(world, commandBuffer);
}

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(ShipPostBornEffect)), Write(typeof(Sprite))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[FineWith(typeof(ApplyTeamColorSystem)), FineWith(typeof(SynchronizeColorSystem))] // 当前系统仅设置透明度和缩放，和应用颜色不冲突
public partial class ApplyShipPostBornEffectSystem(World world, IAssetsManager assets) : ICalcSystem
{
    /// <summary>
    /// 外置的舰船出生后动画。<br/>
    /// 要求的组件为<see cref="Sprite"/>
    /// </summary>
    private readonly AnimationClip<Entity> _shipPostBornAnimationClip = assets.Load<
        AnimationClip<Entity>
    >("Animations/ShipPostBorn.json");

    [Query]
    [All<ShipPostBornEffect, Sprite>]
    private void ApplyBlinkEffect(Entity entity, in ShipPostBornEffect effect)
    {
        var animationTime = (float)effect.TimeElapsed.TotalSeconds;
        var fadeOutTime = effect.TimeElapsed - (Params.PostBornDuration - Params.FadeOutDuration);
        var fadeOutRatio = (float)(fadeOutTime / Params.FadeOutDuration);
        switch (fadeOutRatio)
        {
            case < 0:
                AnimationEvaluator<Entity>.EvaluateAndSet(
                    ref entity,
                    _shipPostBornAnimationClip,
                    animationTime
                );
                break;
            case >= 0 and < 1:
                AnimationEvaluator<Entity>.TweenAndSet(
                    ref entity,
                    _shipPostBornAnimationClip,
                    animationTime,
                    null,
                    float.NaN,
                    null,
                    fadeOutRatio
                );
                break;
        }
    }

    public void Update() => ApplyBlinkEffectQuery(world);
}

#endif

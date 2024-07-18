using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[CoreUpdateSystem]
public partial class UpdateUnitBlinkEffectSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<UnitBlinkEffect>]
    private static void UpdateBlinkEffect(ref UnitBlinkEffect effect, [Data] GameTime time)
    {
        effect.TimeElapsed += time.ElapsedGameTime;
    }
}

[LateUpdateSystem]
public partial class ApplyUnitBlinkEffectSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    /// <summary>
    /// 外置的闪烁动画。<br/>
    /// 要求的组件为<see cref="Sprite"/>
    /// </summary>
    private readonly AnimationClip<Entity> _unitBlinkingAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitBlinking.json");

    [Query]
    [All<UnitBlinkEffect, Sprite>]
    private void ApplyBlinkEffect(Entity entity, in UnitBlinkEffect effect)
    {
        // 计算偏移相位对应的偏移时间
        var offsetTime = _unitBlinkingAnimationClip.Length * effect.PhaseOffset;

        AnimationEvaluator<Entity>.EvaluateAndSet(ref entity, _unitBlinkingAnimationClip,
                                                  (float)effect.TimeElapsed.TotalSeconds + offsetTime);
    }
}

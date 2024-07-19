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

using Params = UnitBornPulseEffectParams;

internal static class UnitBornPulseEffectParams
{
    internal static readonly TimeSpan BornPulseLifetime = TimeSpan.FromSeconds(1);
}

[CoreUpdateSystem]
public partial class UpdateUnitBornPulseEffectSystem(World world, IAssetsManager _)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<UnitBornPulseEffect>]
    private static void UpdateBlinkEffect(ref UnitBornPulseEffect effect, [Data] GameTime time)
    {
        effect.TimeElapsed += time.ElapsedGameTime;
    }
}

[StructuralChangeSystem]
public partial class RemoveUnitBornPulseEffectSystem(World world, IAssetsManager _)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<UnitBornPulseEffect>]
    private void RemoveUnitBornPulseEffect(Entity entity, in UnitBornPulseEffect effect)
    {
        if (effect.TimeElapsed >= Params.BornPulseLifetime)
            _commandBuffer.Destroy(entity);
    }

    public override void Update(in GameTime time)
    {
        RemoveUnitBornPulseEffectQuery(World);
        _commandBuffer.Playback(World);
    }
}


[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyUnitBlinkEffectSystem))]
public partial class ApplyUnitBornPulseEffectSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    /// <summary>
    /// 外置的单位出生后动画。<br/>
    /// 要求的组件为<see cref="Sprite"/>
    /// </summary>
    private readonly AnimationClip<Entity> _unitBornPulseAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitBornPulse.json");

    [Query]
    [All<UnitBornPulseEffect, Sprite>]
    private void ApplyBornPulseEffect(Entity entity, in UnitBornPulseEffect effect)
    {
        var animationTime = (float)effect.TimeElapsed.TotalSeconds;
        AnimationEvaluator<Entity>.EvaluateAndSet(ref entity, _unitBornPulseAnimationClip, animationTime);
    }
}


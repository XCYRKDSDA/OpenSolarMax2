using Arch.Core;
using Arch.System;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

public abstract class HaloExplosionSystemBase : BaseSystem<World, GameTime>
{
    protected HaloExplosionSystemBase(World world, IAssetsManager assets) : base(world)
    {
        _haloExplosionAnimationClip = assets.Load<AnimationClip<Entity>>("Animations/HaloExplosion.json");
        _haloExplosionLifetime = TimeSpan.FromSeconds(_haloExplosionAnimationClip.Length);
    }

    /// <summary>
    /// 外置的爆炸动画。<br/>
    /// 要求的组件为<see cref="Sprite"/>
    /// </summary>
    protected readonly AnimationClip<Entity> _haloExplosionAnimationClip;

    /// <summary>
    /// 爆炸动画生命时长
    /// </summary>
    protected readonly TimeSpan _haloExplosionLifetime;
}

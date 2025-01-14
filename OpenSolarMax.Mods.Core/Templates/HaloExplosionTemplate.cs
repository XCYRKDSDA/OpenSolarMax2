using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class HaloExplosionTemplate : ITemplate
{
    public Archetype Archetype => Archetypes.CountDownAnimation;

    private readonly TextureRegion _haloTexture;

    private readonly AnimationClip<Entity> _explosionAnimation;

    public HaloExplosionTemplate(IAssetsManager assets)
    {
        _haloTexture = assets.Load<TextureRegion>("Textures/Halo.json:Halo");
        _explosionAnimation = assets.Load<AnimationClip<Entity>>("Animations/HaloExplosion.json");
    }

    public void Apply(Entity entity)
    {
        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _haloTexture;
        sprite.Color = Color.White;
        sprite.Alpha = 1;
        sprite.Size = _haloTexture.Bounds.Size.ToVector2();
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _explosionAnimation;
        animation.TimeElapsed = TimeSpan.Zero;
        animation.TimeOffset = TimeSpan.Zero;
    }
}

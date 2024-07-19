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

public class HaloExplosionTemplate(IAssetsManager assets) : ITemplate
{
    public Archetype Archetype => Archetypes.Animation + new Archetype(typeof(HaloExplosionEffect));

    private readonly TextureRegion _haloTexture = assets.Load<TextureRegion>("Textures/Halo.png");

    public void Apply(Entity entity)
    {
        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _haloTexture;
        sprite.Color = Color.White;
        sprite.Alpha = 1;
        sprite.Anchor = new Vector2(109, 105.5f);
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;

        // 设置动画
        ref var effect = ref entity.Get<HaloExplosionEffect>();
        effect.TimeElapsed = TimeSpan.Zero;
    }
}

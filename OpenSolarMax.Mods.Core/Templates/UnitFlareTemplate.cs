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

public class UnitFlareTemplate(IAssetsManager assets) : ITemplate
{
    public Archetype Archetype => Archetypes.CountDownAnimation;

    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipFlare");

    private readonly AnimationClip<Entity> _flareAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/UnitFlare.json");

    public void Apply(Entity entity)
    {
        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _flareTexture;
        sprite.Color = Color.White;
        sprite.Alpha = 1;
        sprite.Anchor = new(148, 148);
        sprite.Scale = Vector2.One * 0.001f;
        sprite.Blend = SpriteBlend.Additive;

        // 设置位姿
        ref var transform = ref entity.Get<RelativeTransform>();
        transform.Translation = Vector3.Zero;
        transform.Rotation = Quaternion.Identity;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _flareAnimation;
        animation.TimeOffset = TimeSpan.Zero;
        animation.TimeElapsed = TimeSpan.Zero;
    }
}

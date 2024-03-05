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

public class UnitPulseTemplate(IAssetsManager assets) : ITemplate
{
    public Archetype Archetype => Archetypes.Animation;

    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipPulse");
    private static readonly AnimationClip<Entity> _pulseAnimation = new();

    private class SpriteAlphaProperty : IProperty<Entity, float>
    {
        public float Get(in Entity obj) => obj.Get<Sprite>().Color.A / 255f;

        public void Set(ref Entity obj, in float value) => obj.Get<Sprite>().Color.A = (byte)(value * 255);
    }

    private class SpriteScaleProperty : IProperty<Entity, Vector2>
    {
        public Vector2 Get(in Entity obj) => obj.Get<Sprite>().Scale;

        public void Set(ref Entity obj, in Vector2 value) => obj.Get<Sprite>().Scale = value;
    }

    static UnitPulseTemplate()
    {
        _pulseAnimation.LoopMode = AnimationLoopMode.RunOnce;
        _pulseAnimation.Length = 0.6f;

        var pulseScaleCurve = new CubicCurve<Vector2>();
        pulseScaleCurve.Keys.Add(new(0.067f, Vector2.One * 0.001f));
        pulseScaleCurve.Keys.Add(new(0.2f, Vector2.One * 0.3f));
        pulseScaleCurve.Keys.Add(new(0.6f, Vector2.One * 0.6f));
        _pulseAnimation.Tracks.Add((new SpriteScaleProperty(), typeof(Vector2)), pulseScaleCurve);

        var pulseAlphaCurve = new CubicCurve<float>();
        pulseAlphaCurve.Keys.Add(new(0.2f, 0.5f, 0));
        pulseAlphaCurve.Keys.Add(new(0.6f, 0, 0));
        _pulseAnimation.Tracks.Add((new SpriteAlphaProperty(), typeof(float)), pulseAlphaCurve);
    }

    public void Apply(Entity entity)
    {
        // 设置颜色
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _pulseTexture;
        sprite.Color = Color.White;
        sprite.Anchor = new(86, 86);
        sprite.Scale = Vector2.One * 0.001f;
        sprite.Blend = SpriteBlend.Additive;

        // 设置位姿
        ref var transform = ref entity.Get<RelativeTransform>();
        transform.Translation = Vector3.Zero;
        transform.Rotation = Quaternion.Identity;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _pulseAnimation;
        animation.LocalTime = 0;
    }
}

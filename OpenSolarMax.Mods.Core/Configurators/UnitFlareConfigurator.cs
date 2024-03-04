using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Configurators;

internal class UnitFlareConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.Animation;

    public Type ConfigurationType => throw new NotImplementedException();

    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipFlare");
    private static readonly AnimationClip<Entity> _flareAnimation = new();

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

    static UnitFlareConfigurator()
    {
        _flareAnimation.LoopMode = AnimationLoopMode.RunOnce;
        _flareAnimation.Length = 0.3f;

        var flareScaleCurve = new CubicCurve<Vector2>();
        flareScaleCurve.Keys.Add(new(0, Vector2.One * 0.001f));
        flareScaleCurve.Keys.Add(new(0.1f, Vector2.One, Vector2.Zero));
        _flareAnimation.Tracks.Add((new SpriteScaleProperty(), typeof(Vector2)), flareScaleCurve);

        var flareAlphaCurve = new CubicCurve<float>();
        flareAlphaCurve.Keys.Add(new(0, 0.25f));
        flareAlphaCurve.Keys.Add(new(0.1f, 0.5f, 0));
        flareAlphaCurve.Keys.Add(new(0.3f, 0, 0));
        _flareAnimation.Tracks.Add((new SpriteAlphaProperty(), typeof(float)), flareAlphaCurve);
    }

    public void Initialize(in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _flareTexture;
        sprite.Anchor = new(148, 148);
        sprite.Scale = Vector2.One * 0.001f;
        sprite.Blend = SpriteBlend.Additive;
        sprite.Color = Color.White;

        // 设置位姿
        ref var transform = ref entity.Get<RelativeTransform>();
        transform.Translation = Vector3.Zero;
        transform.Rotation = Quaternion.Identity;

        // 设置动画
        ref var animation = ref entity.Get<Animation>();
        animation.Clip = _flareAnimation;
        animation.LocalTime = 0;
    }

    public void Configure(IEntityConfiguration configuration, in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        throw new NotImplementedException();
    }
}

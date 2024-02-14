using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Drawing;
using Nine.Graphics;
using OpenSolarMax.Core;
using OpenSolarMax.Core.Components;
using OpenSolarMax.Core.Utils;
using Archetype = OpenSolarMax.Core.Utils.Archetype;

namespace OpenSolarMax.Game.Data;

internal class PlanetConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.Planet;

    public Type ConfigurationType => typeof(PlanetConfiguration);

    private readonly TextureRegion[] _defaultPlanetTextures = (from key in Content.Textures.DefaultPlanetTextures
                                                               select assets.Load<TextureRegion>(key)).ToArray();

    private const float _defaultRadius = 32;
    private const float _defaultOrbitRadius = 64;
    private const float _defaultOrbitPeriod = 10;
    private const float _defaultOrbitMinPitch = -MathF.PI * 11 / 24;
    private const float _defaultOrbitMaxPitch = _defaultOrbitMinPitch + -MathF.PI / 12;
    private const float _defaultOrbitMinRoll = 0;
    private const float _defaultOrbitMaxRoll = _defaultOrbitMinRoll + MathF.PI / 24;

    public void Initialize(in Entity entity, IReadOnlyDictionary<string, Entity> otherEntities)
    {
        var random = new Random();

        ref var sprite = ref entity.Get<Sprite>();
        ref var revolutionOrbit = ref entity.Get<RevolutionOrbit>();
        ref var geostationaryOrbit = ref entity.Get<PlanetGeostationaryOrbit>();

        // 随机填充默认纹理
        var randomIndex = new Random().Next(_defaultPlanetTextures.Length);
        sprite.Texture = _defaultPlanetTextures[randomIndex];
        sprite.Anchor = sprite.Texture.Bounds.Size.ToVector2() / 2;
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Alpha;

        // 默认采用平动
        revolutionOrbit.Mode = RevolutionMode.TranslationOnly;

        // 随机生成同步轨道
        float pitch = (float)random.NextDouble() * (_defaultOrbitMaxPitch - _defaultOrbitMinPitch) + _defaultOrbitMinPitch;
        float roll = (float)random.NextDouble() * (_defaultOrbitMaxRoll - _defaultOrbitMinRoll) + _defaultOrbitMinRoll;
        geostationaryOrbit.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch);
        geostationaryOrbit.Radius = _defaultOrbitRadius;
        geostationaryOrbit.Period = _defaultOrbitPeriod;
    }

    public void Configure(IEntityConfiguration configuration, in Entity entity, IReadOnlyDictionary<string, Entity> otherEntities)
    {
        var planetConfig = (configuration as PlanetConfiguration)!;

        ref var sprite = ref entity.Get<Sprite>();
        ref var relativeTransform = ref entity.Get<RelativeTransform>();
        ref var revolutionOrbit = ref entity.Get<RevolutionOrbit>();
        ref var revolutionState = ref entity.Get<RevolutionState>();
        ref var geostationaryOrbit = ref entity.Get<PlanetGeostationaryOrbit>();

        // 设置星球的尺寸
        if (planetConfig.Radius.HasValue)
        {
            var scale = planetConfig.Radius.Value / _defaultRadius;

            sprite.Scale = new(scale);

            geostationaryOrbit.Radius = _defaultOrbitRadius * scale;
            geostationaryOrbit.Period = _defaultOrbitPeriod * scale;
        }

        // 设置星球的位置
        if (planetConfig.Position.HasValue)
            relativeTransform.Translation = new(planetConfig.Position.Value, 0);

        // 设置星球所在的轨道
        if (planetConfig.Orbit != null)
        {
            if (planetConfig.Orbit.Parent != null)
                entity.SetParent<RelativeTransform>(otherEntities[planetConfig.Orbit.Parent]);

            if (planetConfig.Orbit.Shape.HasValue)
                revolutionOrbit.Shape = (SizeF)planetConfig.Orbit.Shape.Value;

            if (planetConfig.Orbit.Period.HasValue)
                revolutionOrbit.Period = planetConfig.Orbit.Period.Value;

            if (planetConfig.Orbit.Phase.HasValue)
                revolutionState.Phase = planetConfig.Orbit.Phase.Value;
        }
    }
}

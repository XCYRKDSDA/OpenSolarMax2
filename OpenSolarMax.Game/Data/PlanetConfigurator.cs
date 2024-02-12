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

    public void Initialize(in Entity entity, IReadOnlyDictionary<string, Entity> otherEntities)
    {
        ref var sprite = ref entity.Get<Sprite>();
        ref var revolutionOrbit = ref entity.Get<RevolutionOrbit>();

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
    }

    public void Configure(IEntityConfiguration configuration, in Entity entity, IReadOnlyDictionary<string, Entity> otherEntities)
    {
        var planetConfig = (configuration as PlanetConfiguration)!;

        ref var sprite = ref entity.Get<Sprite>();
        ref var relativeTransform = ref entity.Get<RelativeTransform>();
        ref var revolutionOrbit = ref entity.Get<RevolutionOrbit>();
        ref var revolutionState = ref entity.Get<RevolutionState>();

        // 设置星球的尺寸
        if (planetConfig.Radius.HasValue)
            sprite.Scale = Vector2.One * planetConfig.Radius.Value / 64;

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

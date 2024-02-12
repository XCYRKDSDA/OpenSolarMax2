using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Core;
using OpenSolarMax.Core.Components;
using Archetype = OpenSolarMax.Core.Utils.Archetype;

namespace OpenSolarMax.Game.Data;

internal class PlanetConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.Planet;

    public Type ConfigurationType => typeof(PlanetConfiguration);

    private readonly TextureRegion[] _defaultPlanetTextures = (from key in Content.Textures.DefaultPlanetTextures
                                                               select assets.Load<TextureRegion>(key)).ToArray();

    public void Initialize(in Entity entity)
    {
        ref var sprite = ref entity.Get<Sprite>();

        // 随机填充默认纹理
        var randomIndex = new Random().Next(_defaultPlanetTextures.Length);
        sprite.Texture = _defaultPlanetTextures[randomIndex];
        sprite.Anchor = new(0.5f);
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Alpha;
    }

    public void Configure(IEntityConfiguration configuration, in Entity entity)
    {
        var planetConfig = (configuration as PlanetConfiguration)!;

        ref var sprite = ref entity.Get<Sprite>();

        // 设置星球的尺寸
        if (planetConfig.Radius.HasValue)
            sprite.Scale = Vector2.One * planetConfig.Radius.Value / 64;
    }
}

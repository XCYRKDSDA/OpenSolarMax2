﻿using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Core;
using OpenSolarMax.Core.Components;
using OpenSolarMax.Core.Utils;

namespace OpenSolarMax.Game.Data;

internal class ShipConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Core.Utils.Archetype Archetype => Archetypes.Ship;

    public Type ConfigurationType => typeof(ShipConfiguration);

    private readonly TextureRegion _defaultTexture = assets.Load<TextureRegion>(Content.Textures.DefaultShip);

    private const float _defaultRevolutionOffsetRange = 0.3f;

    public void Initialize(in Entity entity, IReadOnlyDictionary<string, Entity> otherEntities)
    {
        ref var sprite = ref entity.Get<Sprite>();

        // 填充默认纹理
        sprite.Texture = _defaultTexture;
        sprite.Anchor = sprite.Texture.Bounds.Size.ToVector2() / 2;
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;
    }

    public void Configure(IEntityConfiguration configuration, in Entity entity, IReadOnlyDictionary<string, Entity> otherEntities)
    {
        var unitConfig = (ShipConfiguration)configuration;

        // 设置所属星球
        if (unitConfig.Planet != null)
        {
            var planetEntity = otherEntities[unitConfig.Planet];
            entity.SetParent<RelativeTransform>(planetEntity);

            var random = new Random();

            // 随机生成轨道
            entity.Get<RevolutionOrbit>() = Revolution.CreateRandomRevolutionOrbit(
                in planetEntity.Get<PlanetGeostationaryOrbit>(), random, _defaultRevolutionOffsetRange);

            // 随机初始化公转状态
            entity.Get<RevolutionState>() = Revolution.CreateRandomState(random);
        }
    }
}
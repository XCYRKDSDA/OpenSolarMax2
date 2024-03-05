﻿using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

/// <summary>
/// 单位实体模板。
/// 将实体配置为一个位于世界系原点的默认尺寸的白色单位
/// </summary>
/// <param name="assets"></param>
internal class ShipTemplate(IAssetsManager assets) : ITemplate
{
    public Archetype Archetype => Archetypes.Ship;

    private readonly TextureRegion _defaultTexture = assets.Load<TextureRegion>(Content.Textures.DefaultShip);

    public void Apply(Entity entity)
    {
        ref var transform = ref entity.Get<RelativeTransform>();
        ref var sprite = ref entity.Get<Sprite>();

        // 置于世界系原点
        transform.Translation = Vector3.Zero;
        transform.Rotation = Quaternion.Identity;

        // 填充默认纹理
        sprite.Texture = _defaultTexture;
        sprite.Color = Color.White;
        sprite.Anchor = sprite.Texture.Bounds.Size.ToVector2() / 2;
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;
    }
}
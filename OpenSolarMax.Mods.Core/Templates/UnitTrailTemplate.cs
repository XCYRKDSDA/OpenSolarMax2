using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

public class UnitTrailTemplate(IAssetsManager assets) : ITemplate
{
    public Archetype Archetype
        => Archetypes.Animation +
           new Archetype(typeof(TrailOf.AsTrail), typeof(InParty.AsAffiliate));

    private readonly TextureRegion _trailTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipTrail");

    public void Apply(Entity entity)
    {
        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _trailTexture;
        sprite.Color = Color.White;
        sprite.Alpha = 0.5f;
        sprite.Size = _trailTexture.Bounds.Size.ToVector2();
        sprite.Scale = new(0.001f, 1);
        sprite.Blend = SpriteBlend.Additive;
    }
}

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

public class UnitBornPulseTemplate(IAssetsManager assets) : ITemplate
{
    public Archetype Archetype { get; } = Archetypes.Animation + new Archetype(typeof(UnitBornPulseEffect));

    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipPulse");

    public void Apply(Entity entity)
    {
        // 设置颜色
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _pulseTexture;
        sprite.Color = Color.White;
        sprite.Alpha = 1;
        sprite.Anchor = new Vector2(86, 86);
        sprite.Scale = Vector2.One * 0.001f;
        sprite.Blend = SpriteBlend.Additive;

        // 设置位姿
        ref var transform = ref entity.Get<RelativeTransform>();
        transform.Translation = Vector3.Zero;
        transform.Rotation = Quaternion.Identity;

        // 初始化效果
        ref var effect = ref entity.Get<UnitBornPulseEffect>();
        effect.TimeElapsed = TimeSpan.Zero;
    }
}

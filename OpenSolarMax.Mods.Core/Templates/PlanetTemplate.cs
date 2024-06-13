using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

/// <summary>
/// 星球模板。
/// 将实体配置为一个位于世界系原点的纹理随机的半径为60的星球；该星球拥有随机同步轨道，且生产速度为0
/// </summary>
/// <param name="assets"></param>
public class PlanetTemplate(IAssetsManager assets) : ITemplate
{
    public Archetype Archetype => Archetypes.Planet;

    private readonly TextureRegion[] _defaultPlanetTextures =
        Content.Textures.DefaultPlanetTextures.Select((k) => assets.Load<TextureRegion>(k)).ToArray();

    private const float _defaultRadius = 60;
    private const float _defaultOrbitRadius = 120;
    private const float _defaultOrbitPeriod = 10;
    private const float _defaultOrbitMinPitch = -MathF.PI * 11 / 24;
    private const float _defaultOrbitMaxPitch = _defaultOrbitMinPitch + MathF.PI / 12;
    private const float _defaultOrbitMinRoll = 0;
    private const float _defaultOrbitMaxRoll = _defaultOrbitMinRoll + MathF.PI / 24;

    public void Apply(Entity entity)
    {
        var random = new Random();

        ref var transform = ref entity.Get<RelativeTransform>();
        ref var sprite = ref entity.Get<Sprite>();
        ref var refSize = ref entity.Get<ReferenceSize>();
        ref var geostationaryOrbit = ref entity.Get<PlanetGeostationaryOrbit>();
        ref var productionAbility = ref entity.Get<ProductionAbility>();

        // 置于世界系原点
        transform.Translation = Vector3.Zero;
        transform.Rotation = Quaternion.Identity;

        // 随机填充默认纹理
        var randomIndex = new Random().Next(_defaultPlanetTextures.Length);
        sprite.Texture = _defaultPlanetTextures[randomIndex];
        sprite.Alpha = 1;
        sprite.Anchor = sprite.Texture.Bounds.Size.ToVector2() / 2;
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Alpha;

        // 默认半径为60
        refSize.Radius = _defaultRadius;

        // 随机生成同步轨道
        float pitch = (float)random.NextDouble() * (_defaultOrbitMaxPitch - _defaultOrbitMinPitch) +
                      _defaultOrbitMinPitch;
        float roll = (float)random.NextDouble() * (_defaultOrbitMaxRoll - _defaultOrbitMinRoll) +
                     _defaultOrbitMinRoll;
        geostationaryOrbit.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll) *
                                      Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch);
        geostationaryOrbit.Radius = _defaultOrbitRadius;
        geostationaryOrbit.Period = _defaultOrbitPeriod;

        // 默认单位生产速度为0；且默认进行生产
        productionAbility.Population = 0;
        productionAbility.ProgressPerSecond = 0;
    }
}

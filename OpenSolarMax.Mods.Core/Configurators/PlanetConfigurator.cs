using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Drawing;
using Nine.Graphics;
using OpenSolarMax.Core;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Configurators;

public class PlanetConfiguration : IEntityConfiguration
{
    /// <summary>
    /// 星球的半径
    /// </summary>
    public float? Radius { get; set; }

    /// <summary>
    /// 星球所在的位置
    /// </summary>
    public Vector2? Position { get; set; }

    public class OrbitConfiguration
    {
        /// <summary>
        /// 星球所围绕的实体
        /// </summary>
        public string? Parent { get; set; }

        /// <summary>
        /// 轨道的形状
        /// </summary>
        public Vector2? Shape { get; set; }

        /// <summary>
        /// 轨道的公转周期
        /// </summary>
        public float? Period { get; set; }

        /// <summary>
        /// 初始时星球在轨道上的相位
        /// </summary>
        public float? Phase { get; set; }
    }

    public OrbitConfiguration? Orbit { get; set; }

    /// <summary>
    /// 星球所属的阵营
    /// </summary>
    public string? Party { get; set; }

    /// <summary>
    /// 该星球可为其阵营提供的人口
    /// </summary>
    public int? Population { get; set; }

    /// <summary>
    /// 该星球生产单位的速度
    /// </summary>
    public float? ProduceSpeed { get; set; }
}

[ConfiguratorKey("planet")]
public class PlanetConfigurator(IAssetsManager assets) : IEntityConfigurator
{
    public Archetype Archetype => Archetypes.Planet;

    public Type ConfigurationType => typeof(PlanetConfiguration);

    private readonly TextureRegion[] _defaultPlanetTextures = (from key in Content.Textures.DefaultPlanetTextures
                                                               select assets.Load<TextureRegion>(key)).ToArray();

    private const float _defaultRadius = 32;
    private const float _defaultOrbitRadius = 64;
    private const float _defaultOrbitPeriod = 10;
    private const float _defaultOrbitMinPitch = -MathF.PI * 11 / 24;
    private const float _defaultOrbitMaxPitch = _defaultOrbitMinPitch + MathF.PI / 12;
    private const float _defaultOrbitMinRoll = 0;
    private const float _defaultOrbitMaxRoll = _defaultOrbitMinRoll + MathF.PI / 24;
    private const string _defaultProductKey = "ship";

    public void Initialize(in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        var random = new Random();

        ref var sprite = ref entity.Get<Sprite>();
        ref var planetSize = ref entity.Get<ReferenceSize>();
        ref var revolutionOrbit = ref entity.Get<RevolutionOrbit>();
        ref var geostationaryOrbit = ref entity.Get<PlanetGeostationaryOrbit>();
        ref var productionAbility = ref entity.Get<ProductionAbility>();

        // 随机填充默认纹理
        var randomIndex = new Random().Next(_defaultPlanetTextures.Length);
        sprite.Texture = _defaultPlanetTextures[randomIndex];
        sprite.Anchor = sprite.Texture.Bounds.Size.ToVector2() / 2;
        sprite.Position = Vector2.Zero;
        sprite.Rotation = 0;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Alpha;

        // 默认直径为纹理的长边长度
        planetSize.Radius = _defaultRadius;

        // 默认采用平动
        revolutionOrbit.Mode = RevolutionMode.TranslationOnly;

        // 随机生成同步轨道
        float pitch = (float)random.NextDouble() * (_defaultOrbitMaxPitch - _defaultOrbitMinPitch) + _defaultOrbitMinPitch;
        float roll = (float)random.NextDouble() * (_defaultOrbitMaxRoll - _defaultOrbitMinRoll) + _defaultOrbitMinRoll;
        geostationaryOrbit.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, roll) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, pitch);
        geostationaryOrbit.Radius = _defaultOrbitRadius;
        geostationaryOrbit.Period = _defaultOrbitPeriod;

        // 默认生成ship单位，但是速度为0
        productionAbility.Population = 0;
        productionAbility.ProgressPerSecond = 0;
        productionAbility.ProductConfigurators = env.Configurators[_defaultProductKey].ToArray();
    }

    public void Configure(IEntityConfiguration configuration, in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        var planetConfig = (configuration as PlanetConfiguration)!;

        ref var sprite = ref entity.Get<Sprite>();
        ref var planetSize = ref entity.Get<ReferenceSize>();
        ref var relativeTransform = ref entity.Get<RelativeTransform>();
        ref var revolutionOrbit = ref entity.Get<RevolutionOrbit>();
        ref var revolutionState = ref entity.Get<RevolutionState>();
        ref var geostationaryOrbit = ref entity.Get<PlanetGeostationaryOrbit>();

        // 设置星球的尺寸
        if (planetConfig.Radius.HasValue)
        {
            planetSize.Radius = planetConfig.Radius.Value;
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
            {
                var parentEntity = ctx.OtherEntities[planetConfig.Orbit.Parent];
                entity.SetParent<RelativeTransform>(parentEntity);

                // 如果指定了一个有预定义轨道的实体作为公转的父级，则采用预定义轨道作为基础值
                if (parentEntity.Has<PredefinedOrbit>())
                    revolutionOrbit = parentEntity.Get<PredefinedOrbit>().Template;
            }

            if (planetConfig.Orbit.Shape.HasValue)
                revolutionOrbit.Shape = (SizeF)planetConfig.Orbit.Shape.Value;

            if (planetConfig.Orbit.Period.HasValue)
                revolutionOrbit.Period = planetConfig.Orbit.Period.Value;

            if (planetConfig.Orbit.Phase.HasValue)
                revolutionState.Phase = planetConfig.Orbit.Phase.Value;
        }

        // 设置所属阵营
        if (planetConfig.Party != null)
            entity.SetParent<Party>(ctx.OtherEntities[planetConfig.Party]);

        // 设置人口
        if (planetConfig.Population.HasValue)
            entity.Get<ProductionAbility>().Population = planetConfig.Population.Value;

        // 设置生产能力
        if (planetConfig.ProduceSpeed.HasValue)
            entity.Get<ProductionAbility>().ProgressPerSecond = planetConfig.ProduceSpeed.Value;
    }
}

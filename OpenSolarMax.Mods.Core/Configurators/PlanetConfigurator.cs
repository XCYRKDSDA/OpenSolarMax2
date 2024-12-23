﻿using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Drawing;
using OpenSolarMax.Game.Data;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;
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
        /// 初始时星球在轨道上的相位。但是以一个周期为单位1
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

    private const string _defaultProductKey = "ship";

    private readonly PlanetTemplate _template = new(assets);

    /// <summary>
    /// 将一个<see cref="IEntityConfigurator"/>及其运行时包装在其中的实体模板
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="ctx"></param>
    /// <param name="env"></param>
    private class ConfiguratorWrapperTemplate(
        IEntityConfigurator configurator,
        WorldLoadingContext ctx, WorldLoadingEnvironment env)
        : ITemplate
    {
        public Archetype Archetype => configurator.Archetype;

        public void Apply(Entity entity) => configurator.Initialize(entity, ctx, env);
    }

    public void Initialize(in Entity entity, WorldLoadingContext ctx, WorldLoadingEnvironment env)
    {
        _template.Apply(entity);

        // 设置星球能够生产的单位的配置器
        ref var productionAbility = ref entity.Get<ProductionAbility>();
        productionAbility.ProductTemplates =
            env.Configurators[_defaultProductKey]
               .Select((c) => new ConfiguratorWrapperTemplate(c, ctx, env) as ITemplate).ToArray();
    }

    public void Configure(IEntityConfiguration configuration, in Entity entity, WorldLoadingContext ctx,
                          WorldLoadingEnvironment env)
    {
        var planetConfig = (configuration as PlanetConfiguration)!;

        ref var sprite = ref entity.Get<Sprite>();
        ref var planetSize = ref entity.Get<ReferenceSize>();
        ref var absoluteTransform = ref entity.Get<AbsoluteTransform>();
        ref var geostationaryOrbit = ref entity.Get<PlanetGeostationaryOrbit>();
        ref var colonizable = ref entity.Get<Colonizable>();

        // 修改星球的尺寸
        if (planetConfig.Radius.HasValue)
        {
            var scale = planetConfig.Radius.Value / planetSize.Radius;
            planetSize.Radius = planetConfig.Radius.Value;

            sprite.Scale *= scale;

            geostationaryOrbit.Radius *= scale;
            geostationaryOrbit.Period *= scale;
        }

        // 设置星球的位置
        if (planetConfig.Position.HasValue)
            absoluteTransform.Translation = new(planetConfig.Position.Value, 0);

        // 设置星球所在的轨道
        if (planetConfig.Orbit != null)
        {
            if (planetConfig.Orbit.Parent != null)
            {
                var world = World.Worlds[entity.WorldId];
                var parentEntity = ctx.OtherEntities[planetConfig.Orbit.Parent];

                // 检查现在是否已有相对变换关系
                ref var transformChild = ref entity.Get<TreeRelationship<RelativeTransform>.AsChild>();

                // 检查是否已与目标构成相对变换关系
                if (transformChild.Relationship?.Copy.Parent == parentEntity)
                    goto DONE;

                // 检查是否已与其他实体构成相对变换关系
                if (transformChild.Relationship?.Copy.Parent is not null)
                {
                    // 目前有和指定父实体不同的关系
                    // TODO - Warning: 新的关系将覆盖老的关系
                    world.Destroy(transformChild.Relationship.Value.Ref);
                    transformChild.Relationship = null;
                }

                // 现在还没有相对变换关系
                var newTransformRelationshipIdx = world.Create(
                    new TreeRelationship<RelativeTransform>(parentEntity.Reference(), entity.Reference()),
                    new RelativeTransform(),
                    new RevolutionOrbit(), new RevolutionState());
                transformChild.Relationship = (newTransformRelationshipIdx.Reference(),
                                               newTransformRelationshipIdx.Get<TreeRelationship<RelativeTransform>>());

                // 如果指定了一个有预定义轨道的实体作为公转的父级，则采用预定义轨道作为基础值
                if (parentEntity.Has<PredefinedOrbit>())
                    newTransformRelationshipIdx.Get<RevolutionOrbit>() = parentEntity.Get<PredefinedOrbit>().Template;

                DONE: ;
            }

            var transformRelationship =
                entity.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship!.Value.Ref;
            ref var revolutionOrbit = ref transformRelationship.Entity.Get<RevolutionOrbit>();
            ref var revolutionState = ref transformRelationship.Entity.Get<RevolutionState>();

            if (planetConfig.Orbit.Shape.HasValue)
                revolutionOrbit.Shape = (SizeF)planetConfig.Orbit.Shape.Value;

            if (planetConfig.Orbit.Period.HasValue)
                revolutionOrbit.Period = planetConfig.Orbit.Period.Value;

            if (planetConfig.Orbit.Phase.HasValue)
                revolutionState.Phase = planetConfig.Orbit.Phase.Value * MathF.PI * 2;
        }

        // 设置所属阵营
        if (planetConfig.Party != null)
        {
            var party = ctx.OtherEntities[planetConfig.Party].Reference();
            World.Worlds[entity.WorldId].Create(new InParty(party, entity.Reference()));
        }

        // 设置人口
        if (planetConfig.Population.HasValue)
            entity.Get<ProductionAbility>().Population = planetConfig.Population.Value;

        // 设置生产能力
        if (planetConfig.ProduceSpeed.HasValue)
            entity.Get<ProductionAbility>().ProgressPerSecond = planetConfig.ProduceSpeed.Value;
    }
}

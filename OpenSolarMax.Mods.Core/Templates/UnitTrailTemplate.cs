using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class UnitTrailTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Entity Unit { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 效果
        typeof(Sprite),
        // 动画
        typeof(Animation),
        //
        typeof(TrailOf.AsTrail)
    );

    public Signature Signature => _signature;

    private readonly TextureRegion _trailTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipTrail");

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _trailTexture;
        sprite.Color = Color.White;
        sprite.Alpha = 0.5f;
        sprite.Size = _trailTexture.Bounds.Size.ToVector2();
        sprite.Scale = new Vector2(0, 1);
        sprite.Blend = SpriteBlend.Additive;

        // 挂载到单位上
        _ = world.Make(new TrailOfTemplate() { Ship = Unit, Trail = entity });
        _ = world.Make(new RelativeTransformTemplate() { Child = entity, Parent = Unit });

        // 设置依赖关系
        _ = world.Make(new DependenceTemplate() { Dependent = entity, Dependency = Unit });
    }

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _trailTexture,
            Color = Color.White,
            Alpha = 0.5f,
            Size = _trailTexture.Bounds.Size.ToVector2(),
            Scale = new Vector2(0, 1),
            Blend = SpriteBlend.Additive
        });

        // 挂载到单位上
        world.Make(commandBuffer, new TrailOfTemplate { Ship = Unit, Trail = entity });
        world.Make(commandBuffer, new RelativeTransformTemplate { Child = entity, Parent = Unit });

        // 设置依赖关系
        world.Make(commandBuffer, new DependenceTemplate { Dependent = entity, Dependency = Unit });
    }
}

using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

public class UnitBornPulseTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Entity Unit { get; set; }

    public required Color Color { get; set; }

    #endregion

    private static readonly Signature _signature = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        // 效果
        typeof(Sprite),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 动画
        typeof(Animation),
        typeof(ExpireAfterAnimationCompleted)
    );

    public Signature Signature => _signature;

    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>("Textures/ShipAtlas.json:ShipPulse");

    private readonly AnimationClip<Entity> _bornPulseAnimationClip =
        assets.Load<AnimationClip<Entity>>("Animations/UnitBornPulse.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置颜色
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _pulseTexture,
            Color = Color,
            Alpha = 1,
            Size = _pulseTexture.Bounds.Size.ToVector2(),
            Scale = Vector2.One * 0.001f,
            Blend = SpriteBlend.Additive
        });

        // 设置动画
        commandBuffer.Set(in entity, new Animation
        {
            Clip = _bornPulseAnimationClip,
            TimeOffset = TimeSpan.Zero,
            TimeElapsed = TimeSpan.Zero
        });

        // 设置相对位置
        world.Make(commandBuffer, new RelativeTransformTemplate { Parent = Unit, Child = entity });

        // 设置依赖关系
        world.Make(commandBuffer, new DependenceTemplate { Dependent = entity, Dependency = Unit });
    }
}

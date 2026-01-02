using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Templates;

internal class PortalChargingBackFlareTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Entity Effect { get; set; }

    public required float Radius { get; set; }

    public required Color Color { get; set; }

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
        typeof(Animation)
    );

    public Signature Signature => _signature;

    private readonly TextureRegion _flareTexture =
        assets.Load<TextureRegion>("Textures/SolarMax2.Atlas.json:SpotGlow");

    private readonly AnimationClip<Entity> _rawFlareCharging =
        assets.Load<AnimationClip<Entity>>("Animations/PortalBackFlareCharging.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 填充默认纹理
        commandBuffer.Set(in entity, new Sprite
        {
            Texture = _flareTexture,
            Color = Color,
            Alpha = 1,
            Size = new(Radius * 2),
            Position = Vector2.Zero,
            Rotation = 0,
            Scale = Vector2.One,
            Blend = SpriteBlend.Additive,
            Billboard = false
        });

        // 初始化动画
        commandBuffer.Set(in entity, new Animation
        {
            TimeElapsed = TimeSpan.Zero,
            TimeOffset = TimeSpan.Zero,
            Clip = _rawFlareCharging
        });

        // 设置到总特效实体的关系
        world.Make(commandBuffer, new DependenceTemplate { Dependent = entity, Dependency = Effect });
        world.Make(commandBuffer, new RelativeTransformTemplate { Parent = Effect, Child = entity });
    }
}

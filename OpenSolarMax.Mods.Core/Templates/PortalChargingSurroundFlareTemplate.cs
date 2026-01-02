using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations.Parametric;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates.Options;

namespace OpenSolarMax.Mods.Core.Templates;

internal class PortalChargingSurroundFlareTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Entity Effect { get; set; }

    public required float Radius { get; set; }

    public required Color Color { get; set; }

    public required float Angle { get; set; }

    public required float MaxSize { get; set; }

    public required float Ratio { get; set; }

    public required float Delay { get; set; }

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

    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>("Textures/SolarMax2.Atlas.json:Halo");

    private readonly ParametricAnimationClip<Entity> _rawFlareRotating =
        assets.Load<ParametricAnimationClip<Entity>>("Animations/PortalSurroundFlareRotating.json");

    private readonly ParametricAnimationClip<Entity> _rawFlareCharging =
        assets.Load<ParametricAnimationClip<Entity>>("Animations/PortalSurroundFlareCharging.json");

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
            Rotation = -MathF.PI / 2,
            Scale = Vector2.One,
            Blend = SpriteBlend.Additive,
            Billboard = false
        });

        // 初始化动画
        _rawFlareCharging.Parameters["MAX_SIZE"] = MaxSize;
        _rawFlareCharging.Parameters["RATIO"] = Ratio;
        commandBuffer.Set(in entity, new Animation
        {
            TimeElapsed = TimeSpan.Zero,
            TimeOffset = TimeSpan.FromSeconds(-Delay),
            Clip = _rawFlareCharging.Bake()
        });

        // 设置到总特效实体的关系
        world.Make(commandBuffer, new DependenceTemplate { Dependent = entity, Dependency = Effect });
        var baseCoord = world.Make(commandBuffer, new EmptyCoordTemplate
        {
            Transform = new RelativeTransformOptions
            {
                Parent = Effect,
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Angle)
            }
        });
        var transform = world.Make(commandBuffer, new RelativeTransformTemplate
        {
            Parent = baseCoord,
            Child = entity
        });

        _rawFlareRotating.Parameters["MAX_SIZE"] = MaxSize;
        _rawFlareRotating.Parameters["RATIO"] = Ratio;
        commandBuffer.Add(in transform, new Animation
        {
            Clip = _rawFlareRotating.Bake(),
            TimeOffset = TimeSpan.FromSeconds(-Delay),
            TimeElapsed = TimeSpan.Zero
        });
    }
}

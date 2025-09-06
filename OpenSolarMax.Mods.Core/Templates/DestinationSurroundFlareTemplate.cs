using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates.Options;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

internal class DestinationSurroundFlareTemplate(IAssetsManager assets) : ITemplate
{
    #region Options

    public required Entity Effect { get; set; }

    public required float Radius { get; set; }

    public required Color Color { get; set; }

    public required float Angle { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
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

    public Archetype Archetype => _archetype;

    private readonly TextureRegion _flareTexture = assets.Load<TextureRegion>("Textures/SolarMax2.Atlas.json:Halo");

    private readonly AnimationClip<Entity> _flareRotating =
        assets.Load<AnimationClip<Entity>>("Animations/DestinationSurroundFlareRotating.json");

    private readonly AnimationClip<Entity> _flareCharging =
        assets.Load<AnimationClip<Entity>>("Animations/DestinationSurroundFlareCharging.json");

    public void Apply(Entity entity)
    {
        var world = World.Worlds[entity.WorldId];

        // 填充默认纹理
        ref var sprite = ref entity.Get<Sprite>();
        sprite.Texture = _flareTexture;
        sprite.Color = Color;
        sprite.Alpha = 1;
        sprite.Size = new(Radius * 2);
        sprite.Position = Vector2.Zero;
        sprite.Rotation = -MathF.PI / 2;
        sprite.Scale = Vector2.One;
        sprite.Blend = SpriteBlend.Additive;
        sprite.Billboard = false;

        // 初始化动画
        ref var animation = ref entity.Get<Animation>();
        animation.TimeElapsed = TimeSpan.Zero;
        animation.TimeOffset = TimeSpan.Zero;
        animation.Clip = _flareCharging;

        // 设置到总特效实体的关系
        _ = world.Make(new DependenceTemplate() { Dependent = entity, Dependency = Effect });
        var baseCoord = world.Make(new EmptyCoordTemplate()
        {
            Transform = new RelativeTransformOptions()
            {
                Parent = Effect,
                Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Angle)
            }
        });
        var transform = world.Make(new RelativeTransformTemplate()
                                       { Parent = baseCoord, Child = entity });
        transform.Add(new Animation()
        {
            Clip = _flareRotating,
            TimeOffset = TimeSpan.Zero,
            TimeElapsed = TimeSpan.Zero
        });
    }
}

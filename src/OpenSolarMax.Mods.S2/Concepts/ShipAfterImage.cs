using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ShipAfterImage = "ShipAfterImage";
}

[Define(ConceptNames.ShipAfterImage)]
public abstract class ShipAfterImageDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new(
            // 位姿变换
            typeof(AbsoluteTransform),
            // 效果
            typeof(Sprite),
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted),
            // 阵营
            typeof(InTeam.AsAffiliate)
        );
}

[Describe(ConceptNames.ShipAfterImage)]
public class ShipAfterImageDescription : IDescription
{
    public Entity Team { get; set; } = Entity.Null;

    public required Vector3 Position { get; set; }

    public required Quaternion Rotation { get; set; }
}

[Apply(ConceptNames.ShipAfterImage)]
public class ShipAfterImageApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<ShipAfterImageDescription>
{
    private readonly TextureRegion _texture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Ship"
    );

    private readonly AnimationClip<Entity> _animation = assets.Load<AnimationClip<Entity>>(
        Content.Animations.ShipAfterImage_json
    );

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipAfterImageDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 摆放位置
        commandBuffer.Set(
            in entity,
            new AbsoluteTransform { Translation = desc.Position, Rotation = desc.Rotation }
        );

        // 设置纹理
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _texture,
                Alpha = 1,
                Size = new(8, 8),
                Scale = Vector2.One,
                Blend = SpriteBlend.Additive,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _animation,
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );

        // 设置阵营
        if (desc.Team != Entity.Null)
            factory.Make(
                world,
                commandBuffer,
                ConceptNames.InTeam,
                new InTeamDescription { Team = desc.Team, Affiliate = entity }
            );
    }
}

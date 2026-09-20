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
    public const string ShipPulse = "ShipPulse";
}

[Define(ConceptNames.ShipPulse)]
public abstract class ShipPulseDefinition : IDefinition
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

[Describe(ConceptNames.ShipPulse)]
public class ShipPulseDescription : IDescription
{
    public required Vector3 Position { get; set; }

    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.ShipPulse)]
public class ShipPulseApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<ShipPulseDescription>
{
    private readonly TextureRegion _pulseTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":ShipPulse"
    );

    private readonly AnimationClip<Entity> _pulseAnimation = assets.Load<AnimationClip<Entity>>(
        Content.Animations.ShipPulse_json
    );

    public void Apply(CommandBuffer commandBuffer, Entity entity, ShipPulseDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 设置位置
        commandBuffer.Set(in entity, new AbsoluteTransform { Translation = desc.Position });

        // 设置颜色
        commandBuffer.Set(
            in entity,
            new Sprite
            {
                Texture = _pulseTexture,
                Alpha = 1,
                Size = new(4, 4),
                Scale = Vector2.Zero,
                Blend = SpriteBlend.Additive,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _pulseAnimation,
                TimeOffset = TimeSpan.Zero,
                TimeElapsed = TimeSpan.Zero,
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

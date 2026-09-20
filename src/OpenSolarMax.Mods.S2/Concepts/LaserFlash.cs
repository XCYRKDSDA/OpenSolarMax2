using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string LaserFlash = "LaserFlash";
}

[Define(ConceptNames.LaserFlash)]
public abstract class LaserFlashDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature
        + TransformableDefinition.Signature
        + TeamInheritableDefinition.Signature
        + new Signature(
            // 效果
            typeof(Sprite),
            // 视觉类型
            typeof(VisualStyle),
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}

[Describe(ConceptNames.LaserFlash)]
public class LaserFlashDescription : IDescription
{
    public Entity Team { get; set; } = Entity.Null;

    public required TextureRegion Texture { get; set; }

    public required Entity Tower { get; set; }
}

[Apply(ConceptNames.LaserFlash)]
public class LaserFlashApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<LaserFlashDescription>
{
    private readonly AnimationClip<Entity> _glowAnimation = assets.Load<AnimationClip<Entity>>(
        Content.Animations.LaserFlash_json
    );

    private readonly TransformableApplier _transformableApplier = new(factory);
    private readonly TeamInheritableApplier _teamApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, LaserFlashDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 摆放位置
        _transformableApplier.Apply(
            commandBuffer,
            entity,
            new TransformableDescription()
            {
                Transform = new RelativeTransformOptions
                {
                    Parent = desc.Tower,
                    Translation = Vector3.UnitZ * 0.1f,
                },
            }
        );

        // 设置纹理
        ref readonly var towerSprite = ref desc.Tower.Get<Sprite>();
        commandBuffer.Set(
            in entity,
            towerSprite with
            {
                Texture = desc.Texture,
                Blend = SpriteBlend.Additive,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _glowAnimation,
                TimeElapsed = TimeSpan.Zero,
                TimeOffset = TimeSpan.Zero,
            }
        );

        // 设置阵营
        _teamApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDescription { Team = desc.Team }
        );

        // 设置视觉类型
        commandBuffer.Set(in entity, VisualStyle.Effect);
    }
}

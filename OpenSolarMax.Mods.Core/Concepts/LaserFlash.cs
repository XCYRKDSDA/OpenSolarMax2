using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;
using Vector3 = System.Numerics.Vector3;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string LaserFlash = "LaserFlash";
}

[Define(ConceptNames.LaserFlash)]
public abstract class LaserFlashDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DependencyCapableDefinition.Signature +
        TransformableDefinition.Signature +
        new Signature(
            // 效果
            typeof(Sprite),
            // 动画
            typeof(Animation),
            typeof(ExpireAfterAnimationCompleted)
        );
}

[Describe(ConceptNames.LaserFlash)]
public class LaserFlashDescription : IDescription
{
    public required Color Color { get; set; }

    public required TextureRegion Texture { get; set; }

    public required Entity Turret { get; set; }
}

[Apply(ConceptNames.LaserFlash)]
public class LaserFlashApplier(IAssetsManager assets, IConceptFactory factory) : IApplier<LaserFlashDescription>
{
    private readonly AnimationClip<Entity> _glowAnimation =
        assets.Load<AnimationClip<Entity>>("Animations/LaserFlash.json");

    public void Apply(CommandBuffer commandBuffer, Entity entity, LaserFlashDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 摆放位置
        factory.Make(world, commandBuffer, ConceptNames.RelativeTransform,
                     new RelativeTransformDescription
                     {
                         Parent = desc.Turret,
                         Child = entity,
                         Translation = Vector3.UnitZ * 0.1f,
                         Rotation = Quaternion.Identity
                     });

        // 设置纹理
        ref readonly var turretSprite = ref desc.Turret.Get<Sprite>();
        commandBuffer.Set(in entity, turretSprite with
        {
            Texture = desc.Texture,
            Color = desc.Color,
            Blend = SpriteBlend.Additive,
        });

        // 设置动画
        commandBuffer.Set(in entity, new Animation
        {
            Clip = _glowAnimation,
            TimeElapsed = TimeSpan.Zero,
            TimeOffset = TimeSpan.Zero
        });
    }
}

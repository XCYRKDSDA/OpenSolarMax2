using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using Nine.Animations;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ColonizationFlare = "ColonizationFlare";
}

[Define(ConceptNames.ColonizationFlare)]
public abstract class ColonizationFlareDefinition : IDefinition
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

[Describe(ConceptNames.ColonizationFlare)]
public class ColonizationFlareDescription : IDescription
{
    public required Entity Planet { get; set; }

    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.ColonizationFlare)]
public class ColonizationFlareApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<ColonizationFlareDescription>
{
    private readonly AnimationClip<Entity> _clip = assets.Load<AnimationClip<Entity>>(
        Content.Animations.ColonizationFlare_json
    );

    private readonly TeamInheritableApplier _teamApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ColonizationFlareDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 挂到行星下（Z 偏移 0.1f）
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.RelativeTransform,
            new RelativeTransformDescription
            {
                Parent = desc.Planet,
                Child = entity,
                Translation = Vector3.UnitZ * 0.1f,
                Rotation = Quaternion.Identity,
            }
        );

        // 依赖行星（行星销毁时 Flare 自动回收）
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.Dependence,
            new DependenceDescription { Dependent = entity, Dependency = desc.Planet }
        );

        // 只读拷贝行星 Sprite，覆写纹理/颜色/混合模式
        ref readonly var planetSprite = ref desc.Planet.Get<Sprite>();
        commandBuffer.Set(
            in entity,
            planetSprite with
            {
                Texture = desc.Planet.Get<Flare>().Texture,
                Blend = SpriteBlend.Additive,
            }
        );

        // 设置动画
        commandBuffer.Set(
            in entity,
            new Animation
            {
                Clip = _clip,
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

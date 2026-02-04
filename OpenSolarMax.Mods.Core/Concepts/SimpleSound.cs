using Arch.Buffer;
using Arch.Core;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string SimpleSound = "SimpleSound";
}

[Define(ConceptNames.SimpleSound)]
public abstract class SimpleSoundDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 音效
        typeof(SoundEffect),
        // 音效结束后死亡
        typeof(ExpireAfterSoundEffectCompleted)
    );
}

[Describe(ConceptNames.SimpleSound)]
public class SimpleSoundDescription : IDescription
{
    public required FmodEventDescription SoundEffect { get; set; }

    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; } =
        new AbsoluteTransformOptions();
}

[Apply(ConceptNames.SimpleSound)]
public class SimpleSoundApplier(IConceptFactory factory) : IApplier<SimpleSoundDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, SimpleSoundDescription desc)
    {
        // 设置位姿
        _transformableApplier.Apply(commandBuffer, entity,
                                    new TransformableDescription() { Transform = desc.Transform });

        // 创建音频实例
        desc.SoundEffect.createInstance(out var eventInstance);
        commandBuffer.Set(in entity, new SoundEffect { EventInstance = eventInstance });
        eventInstance.start();
    }
}

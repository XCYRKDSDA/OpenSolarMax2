using Arch.Core;
using Arch.Core.Extensions;
using OneOf;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates.Options;
using Archetype = OpenSolarMax.Game.Utils.Archetype;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Templates;

public class SimpleSoundTemplate : ITemplate, ITransformableTemplate
{
    #region Options

    public required FmodEventDescription SoundEffect { get; set; }

    public OneOf<AbsoluteTransformOptions, RelativeTransformOptions, RevolutionOptions> Transform { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 位姿变换
        typeof(AbsoluteTransform),
        typeof(TreeRelationship<RelativeTransform>.AsChild),
        typeof(TreeRelationship<RelativeTransform>.AsParent),
        // 音效
        typeof(SoundEffect)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        // 设置位姿
        (this as ITransformableTemplate).Apply(entity);

        // 创建音频实例
        ref var effect = ref entity.Get<SoundEffect>();
        SoundEffect.createInstance(out effect.EventInstance);
        effect.EventInstance.start();
    }
}

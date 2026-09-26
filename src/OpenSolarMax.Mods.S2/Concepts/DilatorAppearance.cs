using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DilatorAppearance = "DilatorAppearance";
}

/// <summary>
/// 临时 dilator 出现事件概念。仅在运行时由触发系统创建
/// </summary>
[Define(ConceptNames.DilatorAppearance)]
public abstract class DilatorAppearanceDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TransformableDefinition.Signature + new Signature(typeof(DilatorAppearanceState));
}

[Describe(ConceptNames.DilatorAppearance)]
public class DilatorAppearanceDescription : IDescription
{
    /// <summary>
    /// 出现事件的位置
    /// </summary>
    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; } = new AbsoluteTransformOptions();

    /// <summary>
    /// 临时 dilator 与舰队所属的阵营
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;

    /// <summary>
    /// 出现时为临时 dilator 生成的舰船数
    /// </summary>
    public int ShipCount { get; set; }
}

[Apply(ConceptNames.DilatorAppearance)]
public class DilatorAppearanceApplier(
    IConceptFactory factory,
    [Section("applier:dilator_appearance")] IConfiguration configs
) : IApplier<DilatorAppearanceDescription>
{
    private readonly TimeSpan _entryDuration = TimeSpan.FromSeconds(
        configs.RequireValue<float>("entry_duration")
    );

    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DilatorAppearanceDescription desc)
    {
        _transformableApplier.Apply(
            commandBuffer,
            entity,
            new TransformableDescription() { Transform = desc.Transform }
        );

        commandBuffer.Set(
            in entity,
            new DilatorAppearanceState
            {
                Phase = DilatorAppearancePhase.Initial,
                TimeLeft = _entryDuration,
                Team = desc.Team,
                ShipCount = desc.ShipCount,
                Dilator = Entity.Null,
            }
        );
    }
}

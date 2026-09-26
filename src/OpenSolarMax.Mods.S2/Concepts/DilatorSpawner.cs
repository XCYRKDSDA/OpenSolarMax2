using Arch.Buffer;
using Arch.Core;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DilatorSpawner = "DilatorSpawner";
}

/// <summary>
/// 临时 dilator 生成点概念。仅作为触发系统的查询依据，没有外观
/// </summary>
[Define(ConceptNames.DilatorSpawner)]
public abstract class DilatorSpawnerDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TransformableDefinition.Signature + new Signature(typeof(DilatorSpawnPoint));
}

[Describe(ConceptNames.DilatorSpawner)]
public class DilatorSpawnerDescription : IDescription
{
    /// <summary>
    /// 生成点的变换关系
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

    /// <summary>
    /// 总人口容量触发阈值
    /// </summary>
    public int TotalPopulationThreshold { get; set; }

    /// <summary>
    /// 总现存人口触发阈值
    /// </summary>
    public int CurrentPopulationThreshold { get; set; }
}

[Apply(ConceptNames.DilatorSpawner)]
public class DilatorSpawnerApplier(IConceptFactory factory) : IApplier<DilatorSpawnerDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DilatorSpawnerDescription desc)
    {
        _transformableApplier.Apply(
            commandBuffer,
            entity,
            new TransformableDescription() { Transform = desc.Transform }
        );

        commandBuffer.Set(
            in entity,
            new DilatorSpawnPoint
            {
                PopulationLimitThreshold = desc.TotalPopulationThreshold,
                CurrentPopulationThreshold = desc.CurrentPopulationThreshold,
                ShipCount = desc.ShipCount,
                Team = desc.Team,
            }
        );
    }
}

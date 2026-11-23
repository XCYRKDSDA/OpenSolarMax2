using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DarkPulseSustainBurst = "DarkPulseSustainBurst";
}

/// <summary>
/// 原版 32 关 dilator 占领后第二段动画演出
/// </summary>
[Define(ConceptNames.DarkPulseSustainBurst)]
public abstract class DarkPulseSustainBurstDefinition : IDefinition
{
    public static Signature Signature { get; } = DarkPulseBurstDefinition.Signature;
}

[Describe(ConceptNames.DarkPulseSustainBurst)]
public class DarkPulseSustainBurstDescription : IDescription
{
    /// <summary>
    /// 演出的中心位置
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// 演出所属的阵营
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;
}

[Apply(ConceptNames.DarkPulseSustainBurst)]
public class DarkPulseSustainBurstApplier(IConceptFactory factory)
    : IApplier<DarkPulseSustainBurstDescription>
{
    // 共 44 轮，尺寸继续 ×0.975
    private const int Rounds = 44;
    private const float MaxSizeGrowth = 0.975f;

    // 初值取第一段末值：20 轮后的冻结值
    private static readonly float InitialInterval = Grow(1f, 0.85f, 20);
    private static readonly float InitialRate = Grow(0.5f, 1.1f, 20);
    private static readonly float InitialMaxSize = Grow(2f, 0.975f, 20);

    private readonly DarkPulseBurstApplier _burstApplier = new(factory);

    /// <summary>
    /// 逐轮连乘求末值
    /// </summary>
    private static float Grow(float value, float growth, int rounds)
    {
        for (var i = 0; i < rounds; i++)
            value *= growth;
        return value;
    }

    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        DarkPulseSustainBurstDescription desc
    )
    {
        _burstApplier.SpawnBurst(
            commandBuffer,
            entity,
            new DarkPulseBurstDescription
            {
                Position = desc.Position,
                Team = desc.Team,
                Rounds = Rounds,
                Direction = DarkPulseBandDirection.Shrink,
                InitialMaxSize = InitialMaxSize,
                MaxSizeGrowth = MaxSizeGrowth,
                InitialRate = InitialRate,
                RateGrowth = 1f,
                InitialInterval = InitialInterval,
                IntervalGrowth = 1f,
            }
        );
    }
}

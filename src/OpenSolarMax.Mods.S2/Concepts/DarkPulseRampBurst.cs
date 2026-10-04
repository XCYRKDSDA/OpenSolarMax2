using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DarkPulseRampBurst = "DarkPulseRampBurst";
}

/// <summary>
/// 原版 32 关 dilator 占领后第一段动画演出
/// </summary>
[Define(ConceptNames.DarkPulseRampBurst)]
public abstract class DarkPulseRampBurstDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DarkPulseBurstDefinition.Signature + new Signature(typeof(ExpiredAfterTimeout));
}

[Describe(ConceptNames.DarkPulseRampBurst)]
public class DarkPulseRampBurstDescription : IDescription
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

[Apply(ConceptNames.DarkPulseRampBurst)]
public class DarkPulseRampBurstApplier(IConceptFactory factory)
    : IApplier<DarkPulseRampBurstDescription>
{
    // 速率 ×1.1、间隔 ×0.85、尺寸 ×0.975 逐轮增长
    private const int Rounds = 20;
    private const float InitialMaxSize = 2f;
    private const float MaxSizeGrowth = 0.975f;
    private const float InitialRate = 0.5f;
    private const float RateGrowth = 1.1f;
    private const float InitialInterval = 1f;
    private const float IntervalGrowth = 0.85f;

    private readonly DarkPulseBurstApplier _burstApplier = new(factory);

    public void Apply(
        CommandBuffer commandBuffer,
        Entity entity,
        DarkPulseRampBurstDescription desc
    )
    {
        var (_, totalSeconds) = _burstApplier.SpawnBurst(
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
                RateGrowth = RateGrowth,
                InitialInterval = InitialInterval,
                IntervalGrowth = IntervalGrowth,
            }
        );

        commandBuffer.Set(
            in entity,
            new ExpiredAfterTimeout
            {
                ElapsedTime = TimeSpan.Zero,
                ExpiryTime = TimeSpan.FromSeconds(totalSeconds) + TimeSpan.FromSeconds(0.1),
            }
        );
    }
}

using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DarkPulseReady = "DarkPulseReady";
}

/// <summary>
/// 临时 dilator 的中场脉冲演出：dilator 亮相那一刻经 <see cref="ConceptNames.DarkPulseBurst"/>
/// 铺出 3 轮生长光带，全部粒子播完后自毁
/// </summary>
[Define(ConceptNames.DarkPulseReady)]
public abstract class DarkPulseReadyDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DarkPulseBurstDefinition.Signature + new Signature(typeof(ExpiredAfterTimeout));
}

[Describe(ConceptNames.DarkPulseReady)]
public class DarkPulseReadyDescription : IDescription
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

[Apply(ConceptNames.DarkPulseReady)]
public class DarkPulseReadyApplier(IConceptFactory factory) : IApplier<DarkPulseReadyDescription>
{
    private readonly DarkPulseBurstApplier _burstApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DarkPulseReadyDescription desc)
    {
        // 3 轮生长光带，轮间尺寸 ×1.5，速率恒定，每颗延时递增 0.05
        var (_, totalSeconds) = _burstApplier.SpawnBurst(
            commandBuffer,
            entity,
            new DarkPulseBurstDescription
            {
                Position = desc.Position,
                Team = desc.Team,
                Rounds = 3,
                Direction = DarkPulseBandDirection.Grow,
                InitialRate = 2f,
                RateGrowth = 1f,
                InitialInterval = 0.05f,
                IntervalGrowth = 1f,
                InitialMaxSize = 1f,
                MaxSizeGrowth = 1.5f,
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

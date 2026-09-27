using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DarkPulseExit = "DarkPulseExit";
}

/// <summary>
/// 临时 dilator 的退场脉冲演出：经 <see cref="ConceptNames.DarkPulseBurst"/> 铺出 12 轮收缩光带，
/// 自行补上收尾一枚光斑，全部粒子播完后自毁
/// </summary>
[Define(ConceptNames.DarkPulseExit)]
public abstract class DarkPulseExitDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DarkPulseBurstDefinition.Signature + new Signature(typeof(ExpiredAfterTimeout));
}

[Describe(ConceptNames.DarkPulseExit)]
public class DarkPulseExitDescription : IDescription
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

[Apply(ConceptNames.DarkPulseExit)]
public class DarkPulseExitApplier(IConceptFactory factory) : IApplier<DarkPulseExitDescription>
{
    /// <summary>
    /// 收尾光斑的尺寸与速率
    /// </summary>
    private const float BlobSize = 2f;
    private const float BlobRate = 2f;

    private readonly DarkPulseBurstApplier _burstApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DarkPulseExitDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 12 轮收缩光带，轮间速率 ×1.5、间隔 ×0.5、尺寸 ×0.7
        var (finalDelay, totalSeconds) = _burstApplier.SpawnBurst(
            commandBuffer,
            entity,
            new DarkPulseBurstDescription
            {
                Position = desc.Position,
                Team = desc.Team,
                Rounds = 12,
                Direction = DarkPulseBandDirection.Shrink,
                InitialRate = 1f,
                RateGrowth = 1.5f,
                InitialInterval = 0.5f,
                IntervalGrowth = 0.5f,
                InitialMaxSize = 2f,
                MaxSizeGrowth = 0.7f,
            }
        );

        // 收尾一枚光斑，开始于最后一轮延迟减 0.75 秒
        var blobDelay = finalDelay - 0.75f;
        totalSeconds = MathF.Max(totalSeconds, blobDelay + BlobSize / BlobRate);
        factory.Make(
            world,
            commandBuffer,
            new DarkPulseBlobDescription
            {
                Position = desc.Position,
                MaxSize = BlobSize,
                Rate = BlobRate,
                Delay = blobDelay,
                Team = desc.Team,
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

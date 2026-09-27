using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DarkPulseEntry = "DarkPulseEntry";
}

/// <summary>
/// 临时 dilator 的入场脉冲演出：经 <see cref="ConceptNames.DarkPulseBurst"/> 铺出 24 轮收缩光带，
/// 自行补上收尾两枚光斑，全部粒子播完后自毁
/// </summary>
[Define(ConceptNames.DarkPulseEntry)]
public abstract class DarkPulseEntryDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DarkPulseBurstDefinition.Signature + new Signature(typeof(ExpiredAfterTimeout));
}

[Describe(ConceptNames.DarkPulseEntry)]
public class DarkPulseEntryDescription : IDescription
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

[Apply(ConceptNames.DarkPulseEntry)]
public class DarkPulseEntryApplier(IConceptFactory factory) : IApplier<DarkPulseEntryDescription>
{
    /// <summary>
    /// 收尾光斑的尺寸与速率
    /// </summary>
    private const float BlobSize = 2f;
    private const float BlobRate = 2f;

    private readonly DarkPulseBurstApplier _burstApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DarkPulseEntryDescription desc)
    {
        var world = World.Worlds[entity.WorldId];

        // 24 轮收缩光带，轮间速率 ×1.15、间隔 ×0.75、尺寸 ×0.9
        var (finalDelay, totalSeconds) = _burstApplier.SpawnBurst(
            commandBuffer,
            entity,
            new DarkPulseBurstDescription
            {
                Position = desc.Position,
                Team = desc.Team,
                Rounds = 24,
                Direction = DarkPulseBandDirection.Shrink,
                InitialRate = 1f,
                RateGrowth = 1.15f,
                InitialInterval = 0.5f,
                IntervalGrowth = 0.75f,
                InitialMaxSize = 2f,
                MaxSizeGrowth = 0.9f,
            }
        );

        // 收尾两枚光斑，分别开始于最后一轮延迟减 0.75 秒与减 0.4 秒
        foreach (var blobDelay in new[] { finalDelay - 0.75f, finalDelay - 0.4f })
        {
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
        }

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

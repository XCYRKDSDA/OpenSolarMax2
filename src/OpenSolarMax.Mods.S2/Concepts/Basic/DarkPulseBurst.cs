using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DarkPulseBurst = "DarkPulseBurst";
}

[Define(ConceptNames.DarkPulseBurst)]
public abstract class DarkPulseBurstDefinition : IDefinition
{
    public static Signature Signature { get; } = TransformableDefinition.Signature;
}

[Describe(ConceptNames.DarkPulseBurst)]
public class DarkPulseBurstDescription : IDescription
{
    /// <summary>
    /// 特效的中心位置
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// 特效所属的阵营
    /// </summary>
    public Entity Team { get; set; } = Entity.Null;

    /// <summary>
    /// 轮数，每轮三枚光带
    /// </summary>
    public required int Rounds { get; set; }

    /// <summary>
    /// 光带的长度变化方向
    /// </summary>
    public DarkPulseBandDirection Direction { get; set; } = DarkPulseBandDirection.Shrink;

    /// <summary>
    /// 光带最大长度的初值
    /// </summary>
    public required float InitialMaxSize { get; set; }

    /// <summary>
    /// 最大长度的逐轮乘数
    /// </summary>
    public required float MaxSizeGrowth { get; set; }

    /// <summary>
    /// 光带长度变化速率的初值。每条光带存活时间为 max size / rate
    /// </summary>
    public required float InitialRate { get; set; }

    /// <summary>
    /// 速率的逐轮乘数
    /// </summary>
    public required float RateGrowth { get; set; }

    /// <summary>
    /// 相邻光带时间间隔的初值
    /// </summary>
    public required float InitialInterval { get; set; }

    /// <summary>
    /// 间隔的逐轮乘数
    /// </summary>
    public required float IntervalGrowth { get; set; }
}

[Apply(ConceptNames.DarkPulseBurst)]
public class DarkPulseBurstApplier(IConceptFactory factory) : IApplier<DarkPulseBurstDescription>
{
    /// <summary>
    /// 相邻光带的夹角增量，每轮三枚绕一圈
    /// </summary>
    private const float AngleStep = MathF.PI * 2 / 3;

    private readonly TransformableApplier _transformableApplier = new(factory);

    /// <summary>
    /// 铺出全部光带，返回末轮结束后的累计延时与最后一枚光带的结束时刻
    /// </summary>
    public (float FinalDelay, float EndTime) SpawnBurst(
        CommandBuffer commandBuffer,
        Entity entity,
        DarkPulseBurstDescription desc
    )
    {
        var world = World.Worlds[entity.WorldId];

        // 特效中心作为全部光带的变换树父级
        _transformableApplier.Apply(
            commandBuffer,
            entity,
            new TransformableDescription()
            {
                Transform = new AbsoluteTransformOptions { Translation = desc.Position },
            }
        );

        var delay = 0f;
        var endTime = 0f;
        var rate = desc.InitialRate;
        var interval = desc.InitialInterval;
        var maxSize = desc.InitialMaxSize;
        var angle = MathF.PI / 2;

        for (var round = 0; round < desc.Rounds; round++)
        {
            for (var i = 0; i < 3; i++)
            {
                endTime = MathF.Max(endTime, delay + maxSize / rate);
                factory.Make(
                    world,
                    commandBuffer,
                    new DarkPulseBandDescription
                    {
                        Effect = entity,
                        MaxSize = maxSize,
                        Rate = rate,
                        Angle = angle,
                        Delay = delay,
                        Direction = desc.Direction,
                        Team = desc.Team,
                    }
                );

                delay += interval;
                angle += AngleStep;
            }

            rate *= desc.RateGrowth;
            interval *= desc.IntervalGrowth;
            maxSize *= desc.MaxSizeGrowth;
        }

        return (delay, endTime);
    }

    public void Apply(CommandBuffer commandBuffer, Entity entity, DarkPulseBurstDescription desc) =>
        SpawnBurst(commandBuffer, entity, desc);
}

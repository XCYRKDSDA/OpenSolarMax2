using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DilatorConversion = "DilatorConversion";
}

/// <summary>
/// 转化事件：dilator 被占领时创建，按原版 32 关的时间线依次铺开暗色脉冲演出，
/// 期间释放转化波并安排退出
/// </summary>
[Define(ConceptNames.DilatorConversion)]
public abstract class DilatorConversionDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TransformableDefinition.Signature
        + new Signature(typeof(DilatorConversionState), typeof(DilatorConversionConfig));
}

[Describe(ConceptNames.DilatorConversion)]
public class DilatorConversionDescription : IDescription
{
    /// <summary>
    /// 演出中心与转化波中心的位置
    /// </summary>
    public Vector3 Position { get; set; } = Vector3.Zero;

    /// <summary>
    /// 转化波的目标阵营
    /// </summary>
    public Entity ConversionTeam { get; set; } = Entity.Null;

    /// <summary>
    /// 转化波半径的扩张速度
    /// </summary>
    public float ConversionSpeed { get; set; }

    /// <summary>
    /// 转化波释放后到退出关卡的延迟
    /// </summary>
    public TimeSpan ExitDelay { get; set; }
}

[Apply(ConceptNames.DilatorConversion)]
public class DilatorConversionApplier(IConceptFactory factory)
    : IApplier<DilatorConversionDescription>
{
    private readonly TransformableApplier _transformableApplier = new(factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DilatorConversionDescription desc)
    {
        // 演出中心即实体位姿
        _transformableApplier.Apply(
            commandBuffer,
            entity,
            new TransformableDescription
            {
                Transform = new AbsoluteTransformOptions { Translation = desc.Position },
            }
        );

        commandBuffer.Set(in entity, new DilatorConversionState { TimeElapsed = TimeSpan.Zero });

        commandBuffer.Set(
            in entity,
            new DilatorConversionConfig
            {
                ConversionTeam = desc.ConversionTeam,
                ConversionSpeed = desc.ConversionSpeed,
                ExitDelay = desc.ExitDelay,
            }
        );
    }
}

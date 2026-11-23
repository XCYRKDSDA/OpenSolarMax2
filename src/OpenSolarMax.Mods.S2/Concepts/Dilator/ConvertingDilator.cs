using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Nine.Assets;
using OneOf;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ConvertingDilator = "ConvertingDilator";
}

/// <summary>
/// 转化 dilator 概念：在 dilator 的基础上，被占领时释放转化波，
/// 把沿途的天体与舰船转为指定阵营。白马非马，独立于 <see cref="ConceptNames.Dilator"/>
/// </summary>
[Define(ConceptNames.ConvertingDilator)]
public abstract class ConvertingDilatorDefinition : IDefinition
{
    public static Signature Signature { get; } =
        DilatorDefinition.Signature
        + new Signature(typeof(DilatorConversionConfig), typeof(ConvertingDilatorState));
}

[Describe(ConceptNames.ConvertingDilator)]
public class ConvertingDilatorDescription : IDescription
{
    /// <summary>
    /// 天体的变换关系
    /// </summary>
    public OneOf<
        AbsoluteTransformOptions,
        RelativeTransformOptions,
        RevolutionOptions
    > Transform { get; set; } = new AbsoluteTransformOptions();

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
    public TimeSpan ExitDelay { get; set; } = TimeSpan.FromSeconds(12.5);
}

[Apply(ConceptNames.ConvertingDilator)]
public class ConvertingDilatorApplier(
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:celestial_body", "applier:dilator")] IConfiguration configs
) : IApplier<ConvertingDilatorDescription>
{
    private readonly DilatorApplier _dilatorApplier = new(assets, factory, configs);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ConvertingDilatorDescription desc)
    {
        _dilatorApplier.Apply(
            commandBuffer,
            entity,
            new DilatorDescription()
            {
                Transform = desc.Transform,
                Team = Entity.Null,
                ProduceShips = false,
                InitialShips = null,
                Capital = false,
                Garrison = 0,
            }
        );

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

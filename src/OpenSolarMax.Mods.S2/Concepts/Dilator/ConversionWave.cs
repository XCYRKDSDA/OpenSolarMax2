using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string ConversionWave = "ConversionWave";
}

/// <summary>
/// 转化波。自 dilator 位置向外扩张，把沿途的天体与舰船转为目标阵营
/// </summary>
[Define(ConceptNames.ConversionWave)]
public abstract class ConversionWaveDefinition : IDefinition
{
    public static Signature Signature { get; } =
        TeamInheritableDrawableDefinition.Signature
        + new Signature(typeof(ConversionWaveConfig), typeof(ConversionWaveState));
}

[Describe(ConceptNames.ConversionWave)]
public class ConversionWaveDescription : IDescription
{
    /// <summary>
    /// 转化波中心的位置
    /// </summary>
    public required Vector3 Position { get; set; }

    /// <summary>
    /// 转化波的目标阵营
    /// </summary>
    public Entity ConversionTeam { get; set; } = Entity.Null;

    /// <summary>
    /// 半径的扩张速度（世界系）
    /// </summary>
    public float ConversionSpeed { get; set; }
}

[Apply(ConceptNames.ConversionWave)]
public class ConversionWaveApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<ConversionWaveDescription>
{
    private readonly TextureRegion _haloTexture = assets.Load<TextureRegion>(
        Content.Textures.SolarMax2_Atlas_json + ":Halo"
    );

    private readonly TeamInheritableDrawableApplier _drawableApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, ConversionWaveDescription desc)
    {
        // 具体尺寸由结算系统逐帧写为当前直径的两倍
        _drawableApplier.Apply(
            commandBuffer,
            entity,
            new TeamInheritableDrawableDescription
            {
                Transform = new AbsoluteTransformOptions { Translation = desc.Position },
                Texture = _haloTexture,
                Blend = SpriteBlend.Additive,
                Team = desc.ConversionTeam,
                VisualStyle = VisualStyle.Effect,
            }
        );

        commandBuffer.Set(
            in entity,
            new ConversionWaveConfig
            {
                ConversionTeam = desc.ConversionTeam,
                ConversionSpeed = desc.ConversionSpeed,
            }
        );
    }
}

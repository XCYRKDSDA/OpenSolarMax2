using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DarkPulseSlowBlob = "DarkPulseSlowBlob";
}

/// <summary>
/// 原版 32 关 dilator 占领后的慢速收尾光斑
/// </summary>
[Define(ConceptNames.DarkPulseSlowBlob)]
public abstract class DarkPulseSlowBlobDefinition : IDefinition
{
    public static Signature Signature { get; } = DarkPulseBlobDefinition.Signature;
}

[Describe(ConceptNames.DarkPulseSlowBlob)]
public class DarkPulseSlowBlobDescription : IDescription
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

[Apply(ConceptNames.DarkPulseSlowBlob)]
public class DarkPulseSlowBlobApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<DarkPulseSlowBlobDescription>
{
    // 尺寸 2.5、速率 0.75
    private const float MaxSize = 2.5f;
    private const float Rate = 0.75f;

    private readonly DarkPulseBlobApplier _blobApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DarkPulseSlowBlobDescription desc)
    {
        _blobApplier.Apply(
            commandBuffer,
            entity,
            new DarkPulseBlobDescription
            {
                Position = desc.Position,
                MaxSize = MaxSize,
                Rate = Rate,
                Team = desc.Team,
            }
        );
    }
}

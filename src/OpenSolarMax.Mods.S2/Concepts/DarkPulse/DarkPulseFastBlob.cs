using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string DarkPulseFastBlob = "DarkPulseFastBlob";
}

/// <summary>
/// 原版 32 关 dilator 占领后的快速收尾光斑
/// </summary>
[Define(ConceptNames.DarkPulseFastBlob)]
public abstract class DarkPulseFastBlobDefinition : IDefinition
{
    public static Signature Signature { get; } = DarkPulseBlobDefinition.Signature;
}

[Describe(ConceptNames.DarkPulseFastBlob)]
public class DarkPulseFastBlobDescription : IDescription
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

[Apply(ConceptNames.DarkPulseFastBlob)]
public class DarkPulseFastBlobApplier(IAssetsManager assets, IConceptFactory factory)
    : IApplier<DarkPulseFastBlobDescription>
{
    // 尺寸 2.5、速率 1
    private const float MaxSize = 2.5f;
    private const float Rate = 1f;

    private readonly DarkPulseBlobApplier _blobApplier = new(assets, factory);

    public void Apply(CommandBuffer commandBuffer, Entity entity, DarkPulseFastBlobDescription desc)
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

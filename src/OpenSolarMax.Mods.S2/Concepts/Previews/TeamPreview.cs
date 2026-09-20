using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Concepts;

public static partial class ConceptNames
{
    public const string TeamPreview = "TeamPreview";
}

[Define(ConceptNames.TeamPreview), OnlyForPreview]
public abstract class TeamPreviewDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new Signature(
            // 阵营参考值
            typeof(RecommendedVisualStyle),
            // 隶属关系
            typeof(InTeam.AsTeam),
            typeof(TeamPopulationRegistry)
        );
}

[Describe(ConceptNames.TeamPreview), OnlyForPreview]
public class TeamPreviewDescription : IDescription
{
    /// <summary>
    /// 阵营的代表色
    /// </summary>
    public required Color Color { get; set; }

    /// <summary>
    /// 属于该阵营的发光外观实体的混合模式
    /// </summary>
    public SpriteBlend Blend { get; set; } = SpriteBlend.Additive;
}

[Apply(ConceptNames.TeamPreview), OnlyForPreview]
public class TeamPreviewApplier : IApplier<TeamPreviewDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, TeamPreviewDescription desc)
    {
        commandBuffer.Set(
            in entity,
            new RecommendedVisualStyle { Color = desc.Color, Blend = desc.Blend }
        );
    }
}

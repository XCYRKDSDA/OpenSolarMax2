using Arch.Buffer;
using Arch.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
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

    /// <summary>
    /// 该阵营天体在预览中的不透明度，null 表示使用默认值
    /// </summary>
    public float? PreviewAlpha { get; set; }
}

[Apply(ConceptNames.TeamPreview), OnlyForPreview]
public class TeamPreviewApplier([Section("applier:celestial_body")] IConfiguration configs)
    : IApplier<TeamPreviewDescription>
{
    private readonly float _defaultPreviewAlpha = configs.RequireValue<float>("preview_alpha");

    public void Apply(CommandBuffer commandBuffer, Entity entity, TeamPreviewDescription desc)
    {
        commandBuffer.Set(
            in entity,
            new RecommendedVisualStyle
            {
                Color = desc.Color,
                Blend = desc.Blend,
                PreviewAlpha = desc.PreviewAlpha ?? _defaultPreviewAlpha,
            }
        );
    }
}

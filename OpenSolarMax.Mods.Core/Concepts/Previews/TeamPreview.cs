using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string TeamPreview = "TeamPreview";
}

[Define(ConceptNames.TeamPreview), OnlyForPreview]
public abstract class TeamPreviewDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new Signature(
            // 阵营参考颜色
            typeof(TeamReferenceColor),
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
}

[Apply(ConceptNames.TeamPreview), OnlyForPreview]
public class TeamPreviewApplier : IApplier<TeamPreviewDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, TeamPreviewDescription desc)
    {
        commandBuffer.Set(in entity, new TeamReferenceColor { Value = desc.Color });
    }
}

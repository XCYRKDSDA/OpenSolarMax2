using Arch.Buffer;
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string PartyPreview = "PartyPreview";
}

[Define(ConceptNames.PartyPreview), OnlyForPreview]
public abstract class PartyPreviewDefinition : IDefinition
{
    public static Signature Signature { get; } =
        new Signature(
            // 阵营参考颜色
            typeof(PartyReferenceColor),
            // 隶属关系
            typeof(InParty.AsParty),
            typeof(PartyPopulationRegistry)
        );
}

[Describe(ConceptNames.PartyPreview), OnlyForPreview]
public class PartyPreviewDescription : IDescription
{
    /// <summary>
    /// 阵营的代表色
    /// </summary>
    public required Color Color { get; set; }
}

[Apply(ConceptNames.PartyPreview), OnlyForPreview]
public class PartyPreviewApplier : IApplier<PartyPreviewDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, PartyPreviewDescription desc)
    {
        commandBuffer.Set(in entity, new PartyReferenceColor { Value = desc.Color });
    }
}

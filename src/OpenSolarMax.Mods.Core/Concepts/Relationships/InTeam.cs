using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string InTeam = "InTeam";
}

[Define(ConceptNames.InTeam), BothForGameplayAndPreview]
public abstract class InTeamDefinition : IDefinition
{
    public static Signature Signature { get; } = new(typeof(InTeam));
}

[Describe(ConceptNames.InTeam), BothForGameplayAndPreview]
public class InTeamDescription : IDescription
{
    public required Entity Team { get; set; }

    public required Entity Affiliate { get; set; }
}

[Apply(ConceptNames.InTeam), BothForGameplayAndPreview]
public class InTeamApplier : IApplier<InTeamDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, InTeamDescription desc)
    {
        commandBuffer.Set(in entity, new InTeam(desc.Team, desc.Affiliate));
    }
}

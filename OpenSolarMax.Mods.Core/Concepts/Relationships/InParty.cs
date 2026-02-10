using Arch.Buffer;
using Arch.Core;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Concepts;

public static partial class ConceptNames
{
    public const string InParty = "InParty";
}

[Define(ConceptNames.InParty)]
public abstract class InPartyDefinition : IDefinition
{
    public static Signature Signature { get; } = new(
        typeof(InParty)
    );
}

[Describe(ConceptNames.InParty)]
public class InPartyDescription : IDescription
{
    public required Entity Party { get; set; }

    public required Entity Affiliate { get; set; }
}

[Apply(ConceptNames.InParty)]
public class InPartyApplier : IApplier<InPartyDescription>
{
    public void Apply(CommandBuffer commandBuffer, Entity entity, InPartyDescription desc)
    {
        commandBuffer.Set(in entity, new InParty(desc.Party, desc.Affiliate));
    }
}

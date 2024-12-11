using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

[Relationship]
public readonly partial struct InParty(in EntityReference party, in EntityReference affiliate)
{
    [Participant(exclusive: false)]
    public readonly EntityReference Party = party;
    
    [Participant]
    public readonly EntityReference Affiliate = affiliate;
}
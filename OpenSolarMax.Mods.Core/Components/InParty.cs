using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

[Relationship]
public readonly partial struct InParty(in Entity party, in Entity affiliate)
{
    [Participant(exclusive: false)]
    public readonly Entity Party = party;

    [Participant]
    public readonly Entity Affiliate = affiliate;
}

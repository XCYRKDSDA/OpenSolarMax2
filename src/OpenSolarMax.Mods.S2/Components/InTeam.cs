using Arch.Core;
using OpenSolarMax.Mods.S2.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

[Relationship]
public readonly partial struct InTeam(in Entity team, in Entity affiliate)
{
    [Participant(exclusive: false)]
    public readonly Entity Team = team;

    [Participant]
    public readonly Entity Affiliate = affiliate;
}

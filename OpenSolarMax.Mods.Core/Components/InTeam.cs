using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

[Relationship]
public readonly partial struct InTeam(in Entity team, in Entity affiliate)
{
    [Participant(exclusive: false)]
    public readonly Entity Team = team;

    [Participant]
    public readonly Entity Affiliate = affiliate;
}

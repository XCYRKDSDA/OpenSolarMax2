using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.Common.Components;

[Relationship]
public readonly partial struct InTeam(in Entity team, in Entity affiliate)
{
    [Participant(exclusive: false)]
    public readonly Entity Team = team;

    [Participant]
    public readonly Entity Affiliate = affiliate;
}

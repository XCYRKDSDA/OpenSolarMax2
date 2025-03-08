using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

[Relationship]
public readonly partial struct Shoot(EntityReference beam, EntityReference target)
{
    [Participant]
    public readonly EntityReference Beam = beam;

    [Participant(exclusive: false)]
    public readonly EntityReference Target = target;
}

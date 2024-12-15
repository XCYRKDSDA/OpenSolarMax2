using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

[Relationship]
public readonly partial struct TreeRelationship<T>(EntityReference parent, EntityReference child)
{
    [Participant(exclusive: false)]
    public readonly EntityReference Parent = parent;

    [Participant]
    public readonly EntityReference Child = child;
}

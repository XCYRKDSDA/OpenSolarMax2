using Arch.Core;
using OpenSolarMax.Mods.S2.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

[Relationship]
public readonly partial struct TreeRelationship<T>(Entity parent, Entity child)
{
    [Participant(exclusive: false)]
    public readonly Entity Parent = parent;

    [Participant]
    public readonly Entity Child = child;
}

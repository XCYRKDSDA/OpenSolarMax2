using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

[Relationship]
public readonly partial struct TreeRelationship<T>(Entity parent, Entity child)
{
    [Participant(exclusive: false)]
    public readonly Entity Parent = parent;

    [Participant]
    public readonly Entity Child = child;
}

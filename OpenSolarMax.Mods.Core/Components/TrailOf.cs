using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

[Relationship]
public partial struct TrailOf(Entity ship, Entity trail)
{
    [Participant]
    public Entity Ship = ship;

    [Participant]
    public Entity Trail = trail;

    public readonly static TrailOf Empty = new(Entity.Null, Entity.Null);
}

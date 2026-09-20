using Arch.Core;
using OpenSolarMax.Mods.S2.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

[Relationship]
public partial struct TrailOf(Entity ship, Entity trail)
{
    [Participant]
    public Entity Ship = ship;

    [Participant]
    public Entity Trail = trail;

    public static readonly TrailOf Empty = new(Entity.Null, Entity.Null);
}

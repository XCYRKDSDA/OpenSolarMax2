using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Components;

[Relationship]
public partial struct TrailOf(EntityReference ship, EntityReference trail)
{
    [Participant]
    public EntityReference Ship = ship;

    [Participant]
    public EntityReference Trail = trail;

    public readonly static TrailOf Empty = new(EntityReference.Null, EntityReference.Null);
}

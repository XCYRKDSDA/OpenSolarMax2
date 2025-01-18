using Arch.Core;
using OpenSolarMax.Mods.Core.SourceGenerators;

namespace OpenSolarMax.Mods.Core.Components;

[Relationship]
public partial struct InPortalEffect
{
    [Participant]
    public EntityReference Portal;

    [Participant]
    public EntityReference SurroundFlare1;

    [Participant]
    public EntityReference SurroundFlare2;

    [Participant]
    public EntityReference SurroundFlare3;

    [Participant]
    public EntityReference BackFlare;
}

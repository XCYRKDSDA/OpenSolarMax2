using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public readonly struct DestinationEffectAssignment(EntityReference[] surroundFlares, EntityReference backFlare)
{
    public readonly EntityReference[] SurroundFlares = surroundFlares;

    public readonly EntityReference BackFlare = backFlare;
}

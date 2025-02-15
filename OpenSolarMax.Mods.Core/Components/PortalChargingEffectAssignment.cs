using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public readonly struct PortalChargingEffectAssignment(
    EntityReference surroundFlare1, EntityReference surroundFlare2, EntityReference surroundFlare3,
    EntityReference backFlare)
{
    public readonly EntityReference[] SurroundFlares = [surroundFlare1, surroundFlare2, surroundFlare3];

    public readonly EntityReference BackFlare = backFlare;
}

using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public struct PortalTransportationTask
{
    public EntityReference Destination;

    public EntityReference Party;

    public int Units;
}

public struct PortalChargingStatus
{
    public PortalTransportationTask Task;

    public EntityReference Effect;
}

public class PortalChargingJobs() : List<PortalChargingStatus>
{ }

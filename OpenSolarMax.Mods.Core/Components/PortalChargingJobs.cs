using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public struct PortalTransportationTask
{
    public Entity Destination;

    public Entity Party;

    public int Units;
}

public struct PortalChargingStatus
{
    public PortalTransportationTask Task;

    public Entity Effect;
}

public class PortalChargingJobs() : List<PortalChargingStatus>
{ }

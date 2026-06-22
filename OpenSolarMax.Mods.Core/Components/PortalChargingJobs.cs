using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public struct PortalTransportationTask
{
    public Entity Destination;

    public Entity Team;

    public int Ships;
}

public struct PortalChargingStatus
{
    public PortalTransportationTask Task;

    public Entity Effect;
}

public class PortalChargingJobs() : List<PortalChargingStatus> { }

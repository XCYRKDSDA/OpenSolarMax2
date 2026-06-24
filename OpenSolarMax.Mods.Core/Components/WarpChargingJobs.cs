using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

public struct WarpChargingTask
{
    public Entity Destination;

    public Entity Team;

    public int Ships;
}

public struct WarpChargingStatus
{
    public WarpChargingTask Task;

    public Entity Effect;
}

public class WarpChargingJobs() : List<WarpChargingStatus> { }

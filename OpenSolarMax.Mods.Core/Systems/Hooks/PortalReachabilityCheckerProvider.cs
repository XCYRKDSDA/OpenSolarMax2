using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[HookProvider]
public static class PortalReachabilityCheckerProvider
{
    [HookOn("CheckPlanetReachability")]
    public static bool? CountReachability(World world,
                                          Entity departure, in AbsoluteTransform departurePose,
                                          Entity destination, in AbsoluteTransform destinationPose)
        => departure.Has<PortalChargingJobs>() ? true : null;

    [HookOn("CheckLocationReachability")]
    public static bool? CountReachability2(World world,
                                           Entity departure, in AbsoluteTransform departurePose,
                                           in Vector3 destinationPose)
        => departure.Has<PortalChargingJobs>() ? true : null;
}

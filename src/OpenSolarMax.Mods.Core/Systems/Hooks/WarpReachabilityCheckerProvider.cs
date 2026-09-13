using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[HookProvider]
public static class WarpReachabilityCheckerProvider
{
    [HookOn("CheckPlanetReachability")]
    public static bool? CountReachability(
        World world,
        Entity departure,
        in AbsoluteTransform departurePose,
        Entity destination,
        in AbsoluteTransform destinationPose
    ) => departure.Has<WarpTerminal>() ? true : null;

    [HookOn("CheckLocationReachability")]
    public static bool? CountReachability2(
        World world,
        Entity departure,
        in AbsoluteTransform departurePose,
        in Vector3 destinationPose
    ) => departure.Has<WarpTerminal>() ? true : null;
}

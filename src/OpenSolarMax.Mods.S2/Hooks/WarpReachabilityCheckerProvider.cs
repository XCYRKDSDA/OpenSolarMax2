using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

[HookProvider]
public static class WarpReachabilityCheckerProvider
{
    [HookOn("CheckLocationReachability")]
    public static bool? CountReachability(
        World world,
        Entity departure,
        in AbsoluteTransform departurePose,
        in Vector3 destinationPose
    ) => departure.Has<WarpTerminal>() ? true : null;
}

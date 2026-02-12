using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;
using Barrier = OpenSolarMax.Mods.Core.Components.Barrier;

namespace OpenSolarMax.Mods.Core.Systems;

public delegate bool? CheckPlanetReachabilityCallback(World world,
                                                      Entity departure, in AbsoluteTransform departurePose,
                                                      Entity destination, in AbsoluteTransform destinationPose);

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(AbsoluteTransform)), ReadCurr(typeof(Barrier)), Write(typeof(ReachabilityRegistry))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public class CountReachabilitySystem(World world) : ICalcSystem
{
    private static readonly QueryDescription _planetDesc = new QueryDescription()
        .WithAll<ReachabilityRegistry, AbsoluteTransform, TreeRelationship<Anchorage>.AsParent>();

    [Hook("CheckPlanetReachability")]
    public CheckPlanetReachabilityCallback? CheckReachabilityDelegate { get; set; }

    private void CountReachability()
    {
        var count = world.CountEntities(in _planetDesc);
        Span<Entity> planets = stackalloc Entity[count];
        world.GetEntities(in _planetDesc, planets);

        for (var i = 0; i < count; i++)
        {
            var planet1 = planets[i];
            ref readonly var registry1 = ref planet1.Get<ReachabilityRegistry>();
            ref readonly var pose1 = ref planet1.Get<AbsoluteTransform>();
            for (var j = i + 1; j < count; j++)
            {
                var planet2 = planets[j];
                ref readonly var registry2 = ref planet2.Get<ReachabilityRegistry>();
                ref readonly var pose2 = ref planet2.Get<AbsoluteTransform>();

                bool? reachability12 = null;
                foreach (var @delegate in CheckReachabilityDelegate?.GetInvocationList() ?? [])
                {
                    var checker = (CheckPlanetReachabilityCallback)@delegate;
                    var result = checker.Invoke(world, planet1, in pose1, planet2, in pose2);
                    if (result is not null)
                    {
                        reachability12 = result.Value;
                        break;
                    }
                }

                bool? reachability21 = null;
                foreach (var @delegate in CheckReachabilityDelegate?.GetInvocationList() ?? [])
                {
                    var checker = (CheckPlanetReachabilityCallback)@delegate;
                    var result = checker.Invoke(world, planet2, in pose2, planet1, in pose1);
                    if (result is not null)
                    {
                        reachability21 = result.Value;
                        break;
                    }
                }

                if (reachability12 is null || reachability21 is null)
                {
                    var reachability =
                        !ManeuveringUtils.CheckBarriersBlocking(world, pose1.Translation, pose2.Translation);
                    reachability12 ??= reachability;
                    reachability21 ??= reachability;
                }

                registry1.FromHereTo[planet2] = registry2.ToHereFrom[planet1] = reachability12!.Value;
                registry2.FromHereTo[planet1] = registry1.ToHereFrom[planet2] = reachability21!.Value;
            }
        }
    }

    public void Update() => CountReachability();
}

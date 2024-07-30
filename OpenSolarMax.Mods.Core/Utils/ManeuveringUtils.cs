using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Mods.Core.Components;
using Barrier = OpenSolarMax.Mods.Core.Components.Barrier;

namespace OpenSolarMax.Mods.Core.Utils;

public static class ManeuveringUtils
{
    private static readonly QueryDescription _barrierDesc = new QueryDescription().WithAll<Barrier>();

    public static bool CheckBarriersBlocking(World world, Vector3 departure, Vector3 destination)
    {
        departure.Z = destination.Z = 0;

        var query = world.Query(in _barrierDesc);
        foreach (var chunk in query.GetChunkIterator())
        {
            var barrierSpan = chunk.GetSpan<Barrier>();
            foreach (var idx in chunk)
            {
                ref readonly var barrier = ref barrierSpan[idx];
                var head = barrier.Head with { Z = 0 };
                var tail = barrier.Tail with { Z = 0 };

                var cross1 = Vector3.Cross(departure - head, destination - head);
                var cross2 = Vector3.Cross(departure - tail, destination - tail);

                if (cross1.Z * cross2.Z <= 0)
                    return true;
            }
        }

        return false;
    }

    public static bool CheckBarriersBlocking(World world, Entity departure, Entity destination)
        => CheckBarriersBlocking(world,
                                 departure.Get<AbsoluteTransform>().Translation,
                                 destination.Get<AbsoluteTransform>().Translation);
}

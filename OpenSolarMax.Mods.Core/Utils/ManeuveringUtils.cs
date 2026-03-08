using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Utils;

public static class ManeuveringUtils
{
    private static readonly QueryDescription _barrierDesc = new QueryDescription().WithAll<InfiniteZBarrier>();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float CrossProduct(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    public static bool CheckBarriersBlocking(World world, Vector2 departure, Vector2 destination)
    {
        var query = world.Query(in _barrierDesc);
        foreach (var chunk in query.GetChunkIterator())
        {
            var barrierSpan = chunk.GetSpan<InfiniteZBarrier>();
            foreach (var idx in chunk)
            {
                ref readonly var barrier = ref barrierSpan[idx];
                var head = barrier.Head;
                var tail = barrier.Tail;

                var cross1 = CrossProduct(departure - head, destination - head);
                var cross2 = CrossProduct(departure - tail, destination - tail);
                var cross3 = CrossProduct(head - departure, tail - departure);
                var cross4 = CrossProduct(head - destination, tail - destination);

                if (cross1 * cross2 <= 0 && cross3 * cross4 <= 0)
                    return true;
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckBarriersBlocking(World world, Vector3 departure, Vector3 destination) =>
        CheckBarriersBlocking(world, new Vector2(departure.X, departure.Y), new Vector2(destination.X, destination.Y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckBarriersBlocking(World world, Entity departure, Entity destination)
        => CheckBarriersBlocking(world,
                                 departure.Get<AbsoluteTransform>().Translation,
                                 destination.Get<AbsoluteTransform>().Translation);
}

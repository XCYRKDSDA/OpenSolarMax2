using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 保证每个出发端到每个目的端之间都存在可达性关系
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(ReachabilityRelation.AsDeparture))]
[ReadCurr(typeof(ReachabilityRelation.AsDestination))]
[ReadCurr(typeof(ReachabilityRelation))]
[DelayedCalc]
public sealed class CreateReachabilityRelationshipsSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    private static readonly QueryDescription _departureDesc =
        new QueryDescription().WithAll<ReachabilityRelation.AsDeparture>();

    private static readonly QueryDescription _destinationDesc =
        new QueryDescription().WithAll<ReachabilityRelation.AsDestination>();

    private static readonly QueryDescription _relationshipDesc =
        new QueryDescription().WithAll<ReachabilityRelation>();

    public void Update(CommandBuffer commandBuffer)
    {
        // 收集全部出发端与目的端
        var departureCount = world.CountEntities(in _departureDesc);
        var departures = new Entity[departureCount];
        world.GetEntities(in _departureDesc, departures);

        var destinationCount = world.CountEntities(in _destinationDesc);
        var destinations = new Entity[destinationCount];
        world.GetEntities(in _destinationDesc, destinations);

        // 收集现存关系实体覆盖的有序组合
        var relationshipCount = world.CountEntities(in _relationshipDesc);
        var relationships = new Entity[relationshipCount];
        world.GetEntities(in _relationshipDesc, relationships);

        var existing = new HashSet<(Entity, Entity)>(relationshipCount);
        foreach (var relationship in relationships)
        {
            ref readonly var record = ref relationship.Get<ReachabilityRelation>();
            existing.Add((record.Departure, record.Destination));
        }

        // 补齐缺失的有向可达性关系
        foreach (var departure in departures)
        {
            foreach (var destination in destinations)
            {
                if (existing.Contains((departure, destination)))
                    continue;

                factory.Make(
                    world,
                    commandBuffer,
                    ConceptNames.ReachabilityRelation,
                    new ReachabilityRelationDescription
                    {
                        Departure = departure,
                        Destination = destination,
                    }
                );
            }
        }
    }
}

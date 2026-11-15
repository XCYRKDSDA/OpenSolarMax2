using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 保证两两天体间一定存在正反向两个可达性关系。
/// 天体死亡后关系实体的清理由 <see cref="DestroyBrokenReachabilityRelationSystem"/> 负责，此处不重复处理
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(ReachabilityRelation.AsDeparture))]
[ReadCurr(typeof(ReachabilityRelation))]
[DelayedCalc]
public sealed class CreateReachabilityRelationshipsSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    private static readonly QueryDescription _celestialBodyDesc =
        new QueryDescription().WithAll<ReachabilityRelation.AsDeparture>();

    private static readonly QueryDescription _relationshipDesc =
        new QueryDescription().WithAll<ReachabilityRelation>();

    public void Update(CommandBuffer commandBuffer)
    {
        // 收集全部天体
        var planetsCount = world.CountEntities(in _celestialBodyDesc);
        var planets = new Entity[planetsCount];
        world.GetEntities(in _celestialBodyDesc, planets);

        // 收集现存关系实体覆盖的有序天体组合
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
        foreach (var departure in planets)
        {
            foreach (var destination in planets)
            {
                if (departure == destination || existing.Contains((departure, destination)))
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

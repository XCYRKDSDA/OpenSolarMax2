using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using CommunityToolkit.HighPerformance;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems.Transportation;

[ReactivelyStructuralChangeSystem]
[ExecuteAfter(typeof(ManageDependenceSystem))]
public sealed partial class FinishPortalChargingSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<PortalChargingJobs, AnchoredShipsRegistry>]
    private void FinishCharging(Entity departure, ref PortalChargingJobs jobs, in AnchoredShipsRegistry ships)
    {
        foreach (ref readonly var job in jobs.AsSpan())
        {
            if (job.Effect.IsAlive())
                continue;

            var shipsRemain = job.Task.Units;
            using var shipsEnumerator = ships.Ships[job.Task.Party].GetEnumerator();
            while (shipsRemain > 0 && shipsEnumerator.MoveNext())
            {
                var ship = shipsEnumerator.Current;

                // 获取相关信息
                var transformRelationship =
                    ship.Entity.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship!.Value.Ref;
                ref readonly var revolutionOrbit = ref transformRelationship.Entity.Get<RevolutionOrbit>();
                ref readonly var revolutionState = ref transformRelationship.Entity.Get<RevolutionState>();
                ref readonly var departurePlanetOrbit = ref departure.Get<PlanetGeostationaryOrbit>();
                ref readonly var destinationPlanetOrbit =
                    ref job.Task.Destination.Entity.Get<PlanetGeostationaryOrbit>();

                // 计算泊入轨道
                var orbitOffset = revolutionOrbit.Shape.X / 2 / departurePlanetOrbit.Radius;
                var expectedOrbit = new RevolutionOrbit()
                {
                    Rotation = destinationPlanetOrbit.Rotation,
                    Shape = new(destinationPlanetOrbit.Radius * orbitOffset * 2,
                                destinationPlanetOrbit.Radius * orbitOffset * 2),
                    Period = destinationPlanetOrbit.Period * MathF.Pow(orbitOffset, 1.5f)
                };

                ref var transportingStatus = ref ship.Entity.Get<TransportingStatus>();
                transportingStatus.State = TransportingState.PreTransportation;
                transportingStatus.Task = new()
                {
                    DestinationPlanet = job.Task.Destination,
                    ExpectedRevolutionOrbit = expectedOrbit,
                    ExpectedRevolutionState = revolutionState
                };
                transportingStatus.PreTransportation = new() { ElapsedTime = TimeSpan.Zero };
            }
        }

        jobs.RemoveAll(j => !j.Effect.IsAlive());
    }
}

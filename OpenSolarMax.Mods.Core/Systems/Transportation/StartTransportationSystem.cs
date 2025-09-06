using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Systems.Transportation;

/// <summary>
/// 处理<see cref="StartShippingRequest"/>使传送门上单位开始传送的系统
/// </summary>
[StructuralChangeSystem]
[ExecuteBefore(typeof(StartShippingSystem))]
public sealed partial class StartTransportationSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    public void ModifyOthers(ISystemProvider systems)
    {
        systems.Get<HandleInputsOnManeuveringShipsSystem>().ReachabilityCheckers.Add(
            (_, departure, _) => departure.Has<PortalChargingJobs>()
        );
        systems.Get<VisualizeManeuveringShipsStatusSystem>().ReachabilityCheckers.Add(
            (_, departure, _) => departure.Has<PortalChargingJobs>()
        );
    }

    [Query]
    [All<StartShippingRequest>]
    private void StartTransporting(Entity requestEntity, in StartShippingRequest request)
    {
        Debug.Assert(requestEntity.WorldId == request.Departure.WorldId
                     && requestEntity.WorldId == request.Destination.WorldId
                     && requestEntity.WorldId == request.Party.WorldId);

        if (!request.Departure.Has<PortalChargingJobs>())
            return;

        // 设置单位传送状态
        var shipsRemain = request.ExpectedNum;
        var allShips = request.Departure.Get<AnchoredShipsRegistry>().Ships[request.Party];
        using var shipsEnumerator = allShips.GetEnumerator();
        while (shipsRemain > 0 && shipsEnumerator.MoveNext())
        {
            var ship = shipsEnumerator.Current;

            // 获取相关信息
            var transformRelationship =
                ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship!.Value.Ref;
            ref readonly var revolutionOrbit = ref transformRelationship.Get<RevolutionOrbit>();
            ref readonly var revolutionState = ref transformRelationship.Get<RevolutionState>();
            ref readonly var departurePlanetOrbit = ref request.Departure.Get<PlanetGeostationaryOrbit>();
            ref readonly var destinationPlanetOrbit = ref request.Destination.Get<PlanetGeostationaryOrbit>();

            // 计算泊入轨道
            var orbitOffset = revolutionOrbit.Shape.X / 2 / departurePlanetOrbit.Radius;
            var expectedOrbit = new RevolutionOrbit()
            {
                Rotation = destinationPlanetOrbit.Rotation,
                Shape = new(destinationPlanetOrbit.Radius * orbitOffset * 2,
                            destinationPlanetOrbit.Radius * orbitOffset * 2),
                Period = destinationPlanetOrbit.Period * MathF.Pow(orbitOffset, 1.5f)
            };

            ref var transportingStatus = ref ship.Get<TransportingStatus>();
            transportingStatus.State = TransportingState.PreTransportation;
            transportingStatus.Task = new()
            {
                DestinationPlanet = request.Destination,
                ExpectedRevolutionOrbit = expectedOrbit,
                ExpectedRevolutionState = revolutionState
            };
            transportingStatus.PreTransportation = new() { ElapsedTime = TimeSpan.Zero };
        }

        // 创建传送门特效
        World.Make(new PortalChargingEffectTemplate(assets)
        {
            Portal = request.Departure,
            PortalRadius = request.Departure.Get<ReferenceSize>().Radius,
            Color = request.Party.Get<PartyReferenceColor>().Value
        });

        _commandBuffer.Destroy(in requestEntity);
    }

    public override void Update(in GameTime t)
    {
        StartTransportingQuery(World);
        _commandBuffer.Playback(World);
    }
}

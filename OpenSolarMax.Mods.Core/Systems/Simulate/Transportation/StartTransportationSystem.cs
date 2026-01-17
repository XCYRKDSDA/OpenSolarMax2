using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Assets;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Systems.Transportation;

/// <summary>
/// 处理<see cref="StartShippingRequest"/>使传送门上单位开始传送的系统
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[ReadPrev(typeof(StartShippingRequest))]
[ReadPrev(typeof(AnchoredShipsRegistry)), ReadPrev(typeof(TreeRelationship<RelativeTransform>.AsChild))]
[ReadPrev(typeof(RevolutionOrbit)), ReadPrev(typeof(RevolutionState)), ReadPrev(typeof(PlanetGeostationaryOrbit))]
[ReadPrev(typeof(ReferenceSize)), ReadPrev(typeof(PartyReferenceColor))]
[Write(typeof(TransportingStatus)), Write(typeof(PortalChargingJobs)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
// 新出发的单位无须更新移动状态，因此要在计算上一帧的移动变化之后发出单位
[ExecuteAfter(typeof(ProgressUnitsTransportationSystem)), ExecuteAfter(typeof(TransportUnitsSystem))]
public sealed partial class StartTransportationSystem(World world, IAssetsManager assets)
    : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<StartShippingRequest>]
    private void StartTransporting(Entity requestEntity, in StartShippingRequest request,
                                   [Data] CommandBuffer commandBuffer)
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
        world.Make(commandBuffer, new PortalChargingEffectTemplate(assets)
        {
            Portal = request.Departure,
            PortalRadius = request.Departure.Get<ReferenceSize>().Radius,
            Color = request.Party.Get<PartyReferenceColor>().Value
        });
    }

    public void Update(CommandBuffer commandBuffer) => StartTransportingQuery(world, commandBuffer);
}

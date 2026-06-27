using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Systems.Warping;

/// <summary>
/// 处理<see cref="StartJumpingRequest"/>使传送门上舰船开始传送的系统
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[
    ReadPrev(typeof(StartJumpingRequest)),
    ReadPrev(typeof(AnchoredShipsRegistry)),
    ReadPrev(typeof(TreeRelationship<RelativeTransform>.AsChild)),
    ReadPrev(typeof(RevolutionOrbit)),
    ReadPrev(typeof(RevolutionState)),
    ReadPrev(typeof(PlanetGeostationaryOrbit)),
    ReadPrev(typeof(ReferenceSize)),
    ReadPrev(typeof(TeamReferenceColor)),
    Write(typeof(WarpingStatus)),
    Write(typeof(WarpChargingJobs)),
    ChangeStructure
]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
// 新出发的舰船无须更新移动状态，因此要在计算上一帧的移动变化之后发出舰船
[ExecuteAfter(typeof(ProgressShipsWarpingSystem)), ExecuteAfter(typeof(WarpSystem))]
public sealed partial class StartWarpingSystem(World world, IConceptFactory factory)
    : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<StartJumpingRequest>]
    private void StartWarping(
        Entity requestEntity,
        in StartJumpingRequest request,
        [Data] CommandBuffer commandBuffer
    )
    {
        Debug.Assert(
            requestEntity.WorldId == request.Departure.WorldId
                && requestEntity.WorldId == request.Destination.WorldId
                && requestEntity.WorldId == request.Team.WorldId
        );

        if (!request.Departure.Has<WarpChargingJobs>())
            return;

        // 设置舰船传送状态
        var shipsRemain = request.ExpectedNum;
        var allShips = request.Departure.Get<AnchoredShipsRegistry>().Ships[request.Team];
        using var shipsEnumerator = allShips.GetEnumerator();
        while (shipsRemain > 0 && shipsEnumerator.MoveNext())
        {
            var ship = shipsEnumerator.Current;
            shipsRemain -= 1;

            // 获取相关信息
            var transformRelationship =
                ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship!.Value.Ref;
            ref readonly var revolutionOrbit = ref transformRelationship.Get<RevolutionOrbit>();
            ref readonly var revolutionState = ref transformRelationship.Get<RevolutionState>();
            ref readonly var departurePlanetOrbit =
                ref request.Departure.Get<PlanetGeostationaryOrbit>();
            ref readonly var destinationPlanetOrbit =
                ref request.Destination.Get<PlanetGeostationaryOrbit>();

            // 计算泊入轨道
            var orbitOffset = revolutionOrbit.Shape.X / 2 / departurePlanetOrbit.Radius;
            var expectedOrbit = new RevolutionOrbit()
            {
                Rotation = destinationPlanetOrbit.Rotation,
                Shape = new(
                    destinationPlanetOrbit.Radius * orbitOffset * 2,
                    destinationPlanetOrbit.Radius * orbitOffset * 2
                ),
                Period = destinationPlanetOrbit.Period * MathF.Pow(orbitOffset, 1.5f),
            };

            ref var warpingStatus = ref ship.Get<WarpingStatus>();
            warpingStatus.State = WarpingState.PreWarp;
            warpingStatus.Task = new()
            {
                DestinationPlanet = request.Destination,
                ExpectedRevolutionOrbit = expectedOrbit,
                ExpectedRevolutionState = revolutionState,
            };
            warpingStatus.PreWarp = new() { ElapsedTime = TimeSpan.Zero };
        }

        // 创建传送门特效
        factory.Make(
            world,
            commandBuffer,
            new WarpChargingEffectDescription()
            {
                Warp = request.Departure,
                WarpRadius = request.Departure.Get<ReferenceSize>().Radius,
                Color = request.Team.Get<TeamReferenceColor>().Value,
            }
        );
    }

    public void Update(CommandBuffer commandBuffer) => StartWarpingQuery(world, commandBuffer);
}

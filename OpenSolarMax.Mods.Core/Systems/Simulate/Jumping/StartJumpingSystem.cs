// 整文件禁用：ECS 框架层重构后待迁移
#if false
using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 处理<see cref="StartJumpingRequest"/>来使舰船开始飞行的系统
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[
    ReadPrev(typeof(StartJumpingRequest)),
    ReadPrev(typeof(AnchoredShipsRegistry)),
    ReadPrev(typeof(Jumpable)),
    ReadPrev(typeof(AbsoluteTransform)),
    ReadPrev(typeof(TreeRelationship<RelativeTransform>.AsChild)),
    ReadPrev(typeof(RevolutionOrbit)),
    ReadPrev(typeof(RevolutionState)),
    ReadPrev(typeof(PlanetGeostationaryOrbit)),
    Iterate(typeof(JumpingStatus)),
    Write(typeof(SoundEffect)),
    ChangeStructure
]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
// 新出发的舰船无须更新移动状态，因此要在计算上一帧的移动变化之后发出舰船
[
    ExecuteAfter(typeof(UpdateShipsStateSystem)),
    ExecuteAfter(typeof(TransitFromChargingToTravellingSystem))
]
// 这一帧刚抵达的舰船不会立刻出发
[ExecuteBefore(typeof(LandArrivedShipsSystem))]
public sealed partial class StartJumpingSystem(
    World world,
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("systems:simulate:jumping")] IConfiguration configs
) : ICalcSystemWithStructuralChanges
{
    private readonly SafeFmodEventDescription _chargingSoundEvent =
        assets.Load<SafeFmodEventDescription>("Sounds/Master.bank:/ShipCharging");

    private readonly float _offsetTime = configs.RequireValue<float>("arrival_time_offset");
    private readonly float _maxOffsetRatio = configs.RequireValue<float>(
        "arrival_time_max_offset_ratio"
    );

    [Query]
    [All<StartJumpingRequest>]
    private void StartJumping(
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

        if (!request.Departure.Has<DefaultLaunchPad>())
            return;

        var shipsRemain = request.ExpectedNum;
        var allShips = request.Departure.Get<AnchoredShipsRegistry>().Ships[request.Team];

        var jumpable = request.Team.Get<Jumpable>();
        var (
            expectedArrivalPlanetPosition,
            expectedTravelDuration,
            arrivalPlanetPositionDerivative
        ) = JumpingUtils.CalculateJumpingTask(request.Departure, request.Destination, jumpable);

        var departurePlanetPosition = request.Departure.Get<AbsoluteTransform>().Translation;
        var departure2Destination = Vector3.Normalize(
            expectedArrivalPlanetPosition - departurePlanetPosition
        );

        using var shipsEnumerator = allShips.GetEnumerator();
        while (shipsRemain > 0 && shipsEnumerator.MoveNext())
        {
            var ship = shipsEnumerator.Current;
            shipsRemain -= 1;

            // 获取相关信息
            ref readonly var pose = ref ship.Get<AbsoluteTransform>();
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
            var expectedPosition =
                expectedArrivalPlanetPosition
                + RevolutionUtils
                    .CalculateTransform(in expectedOrbit, in revolutionState)
                    .Translation;

            // 计算抵达时间偏移
            var departure2Ship = Vector3.Normalize(departurePlanetPosition - pose.Translation);
            var dt = Vector3.Dot(departure2Destination, departure2Ship) * _offsetTime / 2;
            dt = MathHelper.Clamp(
                dt,
                -_maxOffsetRatio * expectedTravelDuration / 2,
                _maxOffsetRatio * expectedTravelDuration / 2
            );

            ref var jumpingStatus = ref ship.Get<JumpingStatus>();

            // 设置任务
            jumpingStatus.Task = new()
            {
                DestinationPlanet = request.Destination,
                ExpectedTravelDuration = expectedTravelDuration + dt,
                DeparturePosition = pose.Translation,
                ExpectedArrivalPosition = expectedPosition + arrivalPlanetPositionDerivative * dt,
                ExpectedRevolutionOrbit = expectedOrbit,
                ExpectedRevolutionState = revolutionState,
            };
            // 初始化状态
            jumpingStatus.State = JumpingState.Charging;
            jumpingStatus.Charging.ElapsedTime = 0;

            // 解除到星球的锚定
            commandBuffer.Destroy(
                ship.Get<TreeRelationship<Anchorage>.AsChild>().Relationship!.Value.Ref
            );
            commandBuffer.Destroy(
                ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship!.Value.Ref
            );

            // 发出声音
            _chargingSoundEvent.Native.createInstance(out var instance);
            ship.Get<SoundEffect>().EventInstance = instance;
            instance.start();

            // 创建舰船的尾迹
            factory.Make(world, commandBuffer, new ShipTrailDescription() { Ship = ship });
        }

        // 移除任务
        commandBuffer.Destroy(in requestEntity);
    }

    public void Update(CommandBuffer commandBuffer) => StartJumpingQuery(world, commandBuffer);
}

#endif

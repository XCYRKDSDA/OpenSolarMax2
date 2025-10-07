using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;
using OpenSolarMax.Mods.Core.Utils;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 处理<see cref="StartShippingRequest"/>来使单位开始飞行的系统
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[ReadPrev(typeof(StartShippingRequest), withEntities: true)]
[ReadPrev(typeof(AnchoredShipsRegistry), withEntities: true), ReadPrev(typeof(Shippable))]
[ReadPrev(typeof(AbsoluteTransform)), ReadPrev(typeof(TreeRelationship<RelativeTransform>.AsChild), withEntities: true)]
[ReadPrev(typeof(RevolutionOrbit)), ReadPrev(typeof(RevolutionState)), ReadPrev(typeof(PlanetGeostationaryOrbit))]
[Iterate(typeof(ShippingStatus)), Write(typeof(SoundEffect)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
// 新出发的单位无须更新移动状态，因此要在计算上一帧的移动变化之后发出单位
[ExecuteAfter(typeof(UpdateShipsStateSystem)), ExecuteAfter(typeof(TransitFromChargingToTravellingSystem))]
// 这一帧刚抵达的单位不会立刻出发
[ExecuteBefore(typeof(LandArrivedShipsSystem))]
public sealed partial class StartShippingSystem(World world, IAssetsManager assets) : ICalcSystemWithStructuralChanges
{
    private FmodEventDescription _chargingSoundEvent =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/ShipCharging");

    private const float _offsetTime = 0.5f;
    private const float _maxOffsetRatio = 0.1f;

    [Query]
    [All<StartShippingRequest>]
    private void StartShipping(Entity requestEntity, in StartShippingRequest request,
                               [Data] CommandBuffer commandBuffer)
    {
        Debug.Assert(requestEntity.WorldId == request.Departure.WorldId
                     && requestEntity.WorldId == request.Destination.WorldId
                     && requestEntity.WorldId == request.Party.WorldId);

        var shipsRemain = request.ExpectedNum;
        var allShips = request.Departure.Get<AnchoredShipsRegistry>().Ships[request.Party];

        var shippable = request.Party.Get<Shippable>();
        var (expectedArrivalPlanetPosition, expectedTravelDuration, arrivalPlanetPositionDerivative) =
            ShippingUtils.CalculateShippingTask(request.Departure, request.Destination, shippable);

        var departurePlanetPosition = request.Departure.Get<AbsoluteTransform>().Translation;
        var departure2Destination = Vector3.Normalize(expectedArrivalPlanetPosition - departurePlanetPosition);

        using var shipsEnumerator = allShips.GetEnumerator();
        while (shipsRemain > 0 && shipsEnumerator.MoveNext())
        {
            var ship = shipsEnumerator.Current;

            // 获取相关信息
            ref readonly var pose = ref ship.Get<AbsoluteTransform>();
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
            var expectedPosition = expectedArrivalPlanetPosition
                                   + RevolutionUtils.CalculateTransform(in expectedOrbit, in revolutionState)
                                                    .Translation;

            // 计算抵达时间偏移
            var departure2Ship = Vector3.Normalize(departurePlanetPosition - pose.Translation);
            var dt = Vector3.Dot(departure2Destination, departure2Ship) * _offsetTime / 2;
            dt = MathHelper.Clamp(dt,
                                  -_maxOffsetRatio * expectedTravelDuration / 2,
                                  _maxOffsetRatio * expectedTravelDuration / 2);

            ref var shippingStatus = ref ship.Get<ShippingStatus>();

            // 设置任务
            shippingStatus.Task = new()
            {
                DestinationPlanet = request.Destination,
                ExpectedTravelDuration = expectedTravelDuration + dt,
                DeparturePosition = pose.Translation,
                ExpectedArrivalPosition = expectedPosition + arrivalPlanetPositionDerivative * dt,
                ExpectedRevolutionOrbit = expectedOrbit,
                ExpectedRevolutionState = revolutionState
            };
            // 初始化状态
            shippingStatus.State = ShippingState.Charging;
            shippingStatus.Charging.ElapsedTime = 0;

            // 解除到星球的锚定
            commandBuffer.Destroy(ship.Get<TreeRelationship<Anchorage>.AsChild>().Relationship!.Value.Ref);
            commandBuffer.Destroy(ship.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship!.Value.Ref);

            // 发出声音
            _chargingSoundEvent.createInstance(out var instance);
            ship.Get<SoundEffect>().EventInstance = instance;
            instance.start();

            // 创建单位的尾迹
            _ = world.Make(commandBuffer, new UnitTrailTemplate(assets) { Unit = ship });
        }

        // 移除任务
        commandBuffer.Destroy(in requestEntity);
    }

    public void Update(CommandBuffer commandBuffer) => StartShippingQuery(world, commandBuffer);
}

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
[StructuralChangeSystem]
public sealed partial class StartShippingSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    private FmodEventDescription _chargingSoundEvent =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/ShipCharging");

    private const float _offsetTime = 0.5f;
    private const float _maxOffsetRatio = 0.1f;

    [Query]
    [All<StartShippingRequest>]
    private void StartShipping(Entity requestEntity, in StartShippingRequest request)
    {
        Debug.Assert(requestEntity.WorldId == request.Departure.Entity.WorldId
                     && requestEntity.WorldId == request.Destination.Entity.WorldId
                     && requestEntity.WorldId == request.Party.Entity.WorldId);

        var shipsRemain = request.ExpectedNum;
        var allShips = request.Departure.Entity.Get<AnchoredShipsRegistry>().Ships[request.Party];

        var shippable = request.Party.Entity.Get<Shippable>();
        var (expectedArrivalPlanetPosition, expectedTravelDuration, arrivalPlanetPositionDerivative) =
            ShippingUtils.CalculateShippingTask(request.Departure, request.Destination, shippable);

        var departurePlanetPosition = request.Departure.Entity.Get<AbsoluteTransform>().Translation;
        var departure2Destination = Vector3.Normalize(expectedArrivalPlanetPosition - departurePlanetPosition);

        var world = World.Worlds[requestEntity.WorldId];
        using var shipsEnumerator = allShips.GetEnumerator();
        while (shipsRemain > 0 && shipsEnumerator.MoveNext())
        {
            var ship = shipsEnumerator.Current;

            // 添加运输任务
            ship.Entity.Add<ShippingTask, ShippingStatus>();
            ref var shippingTask = ref ship.Entity.Get<ShippingTask>();
            ref var shippingState = ref ship.Entity.Get<ShippingStatus>();

            // 获取相关信息
            ref readonly var pose = ref ship.Entity.Get<AbsoluteTransform>();
            var transformRelationship =
                ship.Entity.Get<TreeRelationship<RelativeTransform>.AsChild>().Relationship!.Value.Ref;
            ref readonly var revolutionOrbit = ref transformRelationship.Entity.Get<RevolutionOrbit>();
            ref readonly var revolutionState = ref transformRelationship.Entity.Get<RevolutionState>();
            ref readonly var departurePlanetOrbit = ref request.Departure.Entity.Get<PlanetGeostationaryOrbit>();
            ref readonly var destinationPlanetOrbit = ref request.Destination.Entity.Get<PlanetGeostationaryOrbit>();

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

            // 设置任务
            shippingTask = new ShippingTask()
            {
                DestinationPlanet = request.Destination,
                ExpectedTravelDuration = expectedTravelDuration + dt,
                DeparturePosition = pose.Translation,
                ExpectedArrivalPosition = expectedPosition + arrivalPlanetPositionDerivative * dt,
                ExpectedRevolutionOrbit = expectedOrbit,
                ExpectedRevolutionState = revolutionState
            };
            // 初始化状态
            shippingState.State = ShippingState.Charging;
            shippingState.Charging.ElapsedTime = 0;

            // 解除到星球的锚定
            AnchorageUtils.UnanchorShipFromPlanet(ship, request.Departure);

            // 发出声音
            _chargingSoundEvent.createInstance(out var instance);
            ship.Entity.Get<SoundEffect>().EventInstance = instance;
            instance.start();

            // 创建单位的尾迹
            _ = world.Make(new UnitTrailTemplate(assets) { Unit = ship });
        }

        // 移除任务
        _commandBuffer.Destroy(in requestEntity);
    }

    public override void Update(in GameTime t)
    {
        StartShippingQuery(World);
        _commandBuffer.Playback(World);
    }

    public override void Dispose()
    {
        base.Dispose();
        _commandBuffer.Dispose();
    }
}

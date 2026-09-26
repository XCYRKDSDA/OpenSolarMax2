using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Animations.Parametric;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Utils;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 处理<see cref="StartJumpingRequest"/>来使舰船开始飞行的系统
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(Jumpable))]
[ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(TreeRelationship<RelativeTransform>.AsChild))]
[ReadCurr(typeof(RevolutionOrbit))]
[ReadCurr(typeof(RevolutionState))]
[ReadCurr(typeof(PlanetGeostationaryOrbit))]
[ReadCurr(typeof(InTeam.AsAffiliate))]
[ReadCurr(typeof(StartJumpingRequest))]
[ReadCurr(typeof(StartJumpingAssignedShips))]
[Calc(typeof(SoundEffect))]
[DelayedCalc]
[FineWith(typeof(TransitFromChargingToTravellingSystem), "飞行状态是互斥的", typeof(SoundEffect))]
[FineWith(typeof(LandArrivedShipsSystem), "飞行状态是互斥的", typeof(SoundEffect))]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(SoundEffect))]
public sealed partial class StartJumpingSystem(
    World world,
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("systems:simulate:jumping")] IConfiguration configs
) : IDelayedCalcSystem
{
    private readonly ParametricAnimationClip<Entity> _takingOffClip = assets.Load<
        ParametricAnimationClip<Entity>
    >(Content.Animations.ShipTakingOff_json);

    private readonly SafeFmodEventDescription _chargingSoundEvent =
        assets.Load<SafeFmodEventDescription>($"{Content.Sounds.Master_bank}:/ShipCharging");

    private readonly float _offsetTime = configs.RequireValue<float>("arrival_time_offset");
    private readonly float _maxOffsetRatio = configs.RequireValue<float>(
        "arrival_time_max_offset_ratio"
    );
    private readonly float _chargingStretchMin = configs.RequireValue<float>(
        "charging_stretch_min"
    );
    private readonly float _chargingStretchMax = configs.RequireValue<float>(
        "charging_stretch_max"
    );

    [Query]
    [All<StartJumpingRequest, StartJumpingAssignedShips>]
    private void StartJumping(
        Entity requestEntity,
        in StartJumpingRequest request,
        in StartJumpingAssignedShips assigned,
        [Data] CommandBuffer commandBuffer
    )
    {
        Debug.Assert(
            requestEntity.WorldId == request.Departure.WorldId
                && requestEntity.WorldId == request.Destination.WorldId
                && requestEntity.WorldId == request.Team.WorldId
        );

        // 即使没有默认发射台，如果出发方声明了“回退到普通飞行”（FallsBackToDefaultJumping），
        // 且请求阵营与出发方阵营不同时，则仍然触发起飞流程
        var fallsBackToDefaultJumping =
            request.Departure.Has<FallsBackToDefaultJumping>()
            && (
                request.Departure.Get<InTeam.AsAffiliate>().Relationship
                    is not { Copy.Team: var team }
                || team != request.Team
            );
        if (!request.Departure.Has<DefaultLaunchPad>() && !fallsBackToDefaultJumping)
            return;

        // 零数量请求直接销毁，避免下一轮不动点循环重复触发
        if (request.ExpectedNum <= 0)
        {
            commandBuffer.Destroy(in requestEntity);
            return;
        }

        Debug.Assert(assigned.Ships is not null);

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

        foreach (var ship in assigned.Ships)
        {
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

            // 拉长段的随机耗时。超出基准（区间下限）的部分不计入飞行时长补偿，
            // 直接推迟抵达时刻
            var stretch = MathHelper.Lerp(
                _chargingStretchMin,
                _chargingStretchMax,
                (float)Random.Shared.NextDouble()
            );
            var stretchDelay = stretch - _chargingStretchMin;
            var arrivalTimeOffset = dt + stretchDelay;

            // Debug.WriteLine(
            //     $"{ship.Id},{pose.Translation.X},{pose.Translation.Y},{pose.Translation.Z},{expectedPosition.X},{expectedPosition.Y},{expectedPosition.Z},{dt}"
            // );

            // 设置任务并初始化状态。JumpingStatus 的写入经命令缓冲延迟生效，
            // 用于打破 StartJumpingSystem 与 CalculateShipPositionSystem 之间的读写环
            commandBuffer.Set(
                ship,
                new JumpingStatus()
                {
                    State = JumpingState.Charging,
                    Task = new()
                    {
                        DestinationPlanet = request.Destination,
                        ExpectedTravelDuration = expectedTravelDuration + arrivalTimeOffset,
                        DeparturePosition = pose.Translation,
                        ExpectedArrivalPosition =
                            expectedPosition + arrivalPlanetPositionDerivative * arrivalTimeOffset,
                        ExpectedRevolutionOrbit = expectedOrbit,
                        ExpectedRevolutionState = revolutionState,
                    },
                    Charging = new()
                    {
                        ElapsedTime = 0,
                        Clip = _takingOffClip.Bake(
                            new Dictionary<string, object?> { ["STRETCH"] = stretch }
                        ),
                    },
                }
            );

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
        }

        // 移除任务
        commandBuffer.Destroy(in requestEntity);
    }

    public void Update(CommandBuffer commandBuffer) => StartJumpingQuery(world, commandBuffer);
}

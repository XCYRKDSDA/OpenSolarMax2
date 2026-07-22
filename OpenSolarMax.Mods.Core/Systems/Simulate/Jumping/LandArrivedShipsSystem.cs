using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 考察移动进度，将舰船降落到目标星球的系统
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(TrailOf.AsShip))]
[Write(typeof(JumpingStatus))]
[Write(typeof(SoundEffect))]
[ChangeStructure]
[ExecuteAfter(typeof(TransitFromChargingToTravellingSystem))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class LandArrivedShipsSystem(
    World world,
    IAssetsManager assets,
    IConceptFactory factory
) : ICalcSystemWithStructuralChanges
{
    private readonly List<Entity> _arrivedEntities = [];

    private readonly SafeFmodEventDescription _travelDoneSoundEvent =
        assets.Load<SafeFmodEventDescription>("Sounds/Master.bank:/ShipDone");

    [Query]
    [All<JumpingStatus>]
    private static void FindArrivedShips(
        Entity ship,
        in JumpingStatus status,
        [Data] List<Entity> arrivedEntities
    )
    {
        if (status.State == JumpingState.Idle)
            return;

        if (status.State != JumpingState.Travelling)
            return;

        if (
            status.Travelling.ElapsedTime + status.Travelling.DelayedTime
            >= status.Task.ExpectedTravelDuration
        )
            arrivedEntities.Add(ship);
    }

    private void LandShip(
        Entity ship,
        ref JumpingStatus status,
        ref SoundEffect soundEffect,
        CommandBuffer commandBuffer
    )
    {
        // 结束飞行
        status.State = JumpingState.Idle;

        // 将舰船挂载到目标星球
        factory.Make(
            world,
            commandBuffer,
            new AnchorageDescription() { Planet = status.Task.DestinationPlanet, Ship = ship }
        );

        // 创建公转关系
        factory.Make(
            world,
            commandBuffer,
            new RevolutionDescription()
            {
                Parent = status.Task.DestinationPlanet,
                Child = ship,
                Shape = status.Task.ExpectedRevolutionOrbit.Shape,
                Period = status.Task.ExpectedRevolutionOrbit.Period,
                Rotation = status.Task.ExpectedRevolutionOrbit.Rotation,
                InitPhase = status.Task.ExpectedRevolutionState.Phase,
            }
        );

        // 销毁舰船的尾迹实体
        commandBuffer.Destroy(ship.Get<TrailOf.AsShip>().Relationship!.Value.Copy.Trail);

        // 播放音效
        _travelDoneSoundEvent.Native.createInstance(out var instance);
        soundEffect.EventInstance = instance;
        instance.start();
    }

    public void Update(CommandBuffer commandBuffer)
    {
        FindArrivedShipsQuery(world, _arrivedEntities);

        foreach (var entity in _arrivedEntities)
        {
            var refs = entity.Get<JumpingStatus, SoundEffect>();
            LandShip(entity, ref refs.t0, ref refs.t1, commandBuffer);
        }
        _arrivedEntities.Clear();
    }
}

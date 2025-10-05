using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 考察移动进度，将单位降落到目标星球的系统
/// </summary>
[SimulateSystem]
[Write(typeof(ShippingStatus)), Read(typeof(TrailOf.AsShip), withEntities: true)]
[Write(typeof(SoundEffect))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(TransitFromChargingToTravellingSystem))] // 以防一帧就抵达
public sealed partial class LandArrivedShipsSystem(World world, IAssetsManager assets)
    : ILateUpdateWithStructuralChangesSystem
{
    private readonly List<Entity> _arrivedEntities = [];

    private FmodEventDescription _travelDoneSoundEvent =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/ShipDone");

    [Query]
    [All<ShippingStatus>]
    private static void FindArrivedShips(Entity ship, in ShippingStatus status, [Data] List<Entity> arrivedEntities)
    {
        if (status.State == ShippingState.Idle) return;

        if (status.State != ShippingState.Travelling) return;

        if (status.Travelling.ElapsedTime + status.Travelling.DelayedTime >= status.Task.ExpectedTravelDuration)
            arrivedEntities.Add(ship);
    }

    private void LandShip(Entity ship, ref ShippingStatus status, ref SoundEffect soundEffect,
                          CommandBuffer commandBuffer)
    {
        // 结束飞行
        status.State = ShippingState.Idle;

        // 将单位挂载到目标星球
        world.Make(commandBuffer, new AnchorageTemplate()
        {
            Planet = status.Task.DestinationPlanet,
            Ship = ship,
        });

        // 创建公转关系
        world.Make(commandBuffer, new RevolutionTemplate()
        {
            Parent = status.Task.DestinationPlanet,
            Child = ship,
            Shape = status.Task.ExpectedRevolutionOrbit.Shape,
            Period = status.Task.ExpectedRevolutionOrbit.Period,
            Rotation = status.Task.ExpectedRevolutionOrbit.Rotation,
            InitPhase = status.Task.ExpectedRevolutionState.Phase,
        });

        // 销毁单位的尾迹实体
        commandBuffer.Destroy(ship.Get<TrailOf.AsShip>().Relationship!.Value.Copy.Trail);

        // 播放音效
        _travelDoneSoundEvent.createInstance(out var instance);
        soundEffect.EventInstance = instance;
        instance.start();
    }

    public void Update(CommandBuffer commandBuffer)
    {
        FindArrivedShipsQuery(world, _arrivedEntities);

        foreach (var entity in _arrivedEntities)
        {
            var refs = entity.Get<ShippingStatus, SoundEffect>();
            LandShip(entity, ref refs.t0, ref refs.t1, commandBuffer);
        }
        _arrivedEntities.Clear();
    }
}

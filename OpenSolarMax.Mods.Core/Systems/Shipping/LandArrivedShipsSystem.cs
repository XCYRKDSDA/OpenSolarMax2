using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;
using FmodEventDescription = FMOD.Studio.EventDescription;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 考察移动进度，将单位降落到目标星球的系统
/// </summary>
[StructuralChangeSystem]
[ExecuteAfter(typeof(StartShippingSystem))]
public sealed partial class LandArrivedShipsSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
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

    private void LandShip(Entity ship, ref ShippingStatus status, ref SoundEffect soundEffect)
    {
        // 结束飞行
        status.State = ShippingState.Idle;

        // 将单位挂载到目标星球
        var (_, transformRelationship) = AnchorageUtils.AnchorShipToPlanet(ship, status.Task.DestinationPlanet);
        transformRelationship.Set(status.Task.ExpectedRevolutionOrbit, status.Task.ExpectedRevolutionState);

        // 销毁单位的尾迹实体
        var world = World.Worlds[ship.WorldId];
        world.Destroy(ship.Get<TrailOf.AsShip>().Relationship!.Value.Copy.Trail);

        // 播放音效
        _travelDoneSoundEvent.createInstance(out var instance);
        soundEffect.EventInstance = instance;
        instance.start();
    }

    public override void Update(in GameTime t)
    {
        FindArrivedShipsQuery(World, _arrivedEntities);

        foreach (var entity in _arrivedEntities)
        {
            var refs = entity.Get<ShippingStatus, SoundEffect>();
            LandShip(entity, ref refs.t0, ref refs.t1);
        }
        _arrivedEntities.Clear();
    }
}

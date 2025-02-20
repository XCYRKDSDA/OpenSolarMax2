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
/// 处理<see cref="StartShippingRequest"/>来使传送门开始传送的系统
/// </summary>
[StructuralChangeSystem]
[ExecuteBefore(typeof(StartShippingSystem))]
public sealed partial class StartPortalChargingSystem(World world, IAssetsManager assets)
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
    private void StartCharging(Entity requestEntity, in StartShippingRequest request)
    {
        Debug.Assert(requestEntity.WorldId == request.Departure.Entity.WorldId
                     && requestEntity.WorldId == request.Destination.Entity.WorldId
                     && requestEntity.WorldId == request.Party.Entity.WorldId);

        if (!request.Departure.Entity.Has<PortalChargingJobs>())
            return;
        _commandBuffer.Destroy(in requestEntity);

        var portal = request.Departure;
        ref var jobs = ref portal.Entity.Get<PortalChargingJobs>();
        jobs.Add(new()
        {
            Task = new()
            {
                Destination = request.Destination,
                Party = request.Party,
                Units = request.ExpectedNum
            },
            Effect = World.Make(new PortalChargingEffectTemplate(assets)
            {
                Portal = portal,
                PortalRadius = portal.Entity.Get<ReferenceSize>().Radius,
                Color = request.Party.Entity.Get<PartyReferenceColor>().Value
            }).Reference()
        });
    }

    public override void Update(in GameTime t)
    {
        StartChargingQuery(World);
        _commandBuffer.Playback(World);
    }
}

using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(ShipDeathState)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    DelayedCalc
]
public sealed partial class PlayShipDeathEffectSystem(World world, IConceptFactory factory)
    : IDelayedCalcSystem
{
    [Query]
    [All<ShipDeathState, AbsoluteTransform, InTeam.AsAffiliate>]
    private void PlayEffect(
        ref ShipDeathState deathState,
        in AbsoluteTransform transform,
        in InTeam.AsAffiliate asAffiliate,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (deathState.State != DeathState.Dying)
            return;

        var position = transform.Translation;
        var team = asAffiliate.Relationship?.Copy.Team ?? Entity.Null;

        factory.Make(
            world,
            commandBuffer,
            new ShipFlareDescription { Team = team, Position = position }
        );

        factory.Make(
            world,
            commandBuffer,
            new ShipPulseDescription { Team = team, Position = position }
        );

        deathState.State = DeathState.Dead;
    }

    public void Update(CommandBuffer commandBuffer) => PlayEffectQuery(world, commandBuffer);
}

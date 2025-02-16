using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 推进殖民进度的系统
/// </summary>
[CoreUpdateSystem]
public sealed partial class ProgressColonizationSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ColonizationState, Colonizable, InParty.AsAffiliate, AnchoredShipsRegistry>]
    private static void UpdateColonization([Data] GameTime time,
                                           ref ColonizationState state, in Colonizable colonizable,
                                           in AnchoredShipsRegistry shipsRegistry)
    {
        if (shipsRegistry.Ships.Count != 1)
        {
            state.Event = ColonizationEvent.Idle;
            return;
        }

        var colonizeParty = shipsRegistry.Ships.First().Key;
        var shipsNum = shipsRegistry.Ships.First().Count();

        var deltaProgress = shipsNum * colonizeParty.Entity.Get<ColonizationAbility>().ProgressPerSecond
                                     * (float)time.ElapsedGameTime.TotalSeconds;

        if (state.Party == colonizeParty || state.Party == EntityReference.Null)
        {
            if (state.Progress >= colonizable.Volume)
                state.Event = ColonizationEvent.Idle;
            else
            {
                state.Party = colonizeParty;
                state.Progress += deltaProgress;

                if (state.Progress > colonizable.Volume)
                {
                    state.Progress = colonizable.Volume;
                    state.Event = ColonizationEvent.Finished;
                }
                else
                    state.Event = ColonizationEvent.Progressing;
            }
        }
        else
        {
            state.Progress -= deltaProgress;

            if (state.Progress < 0)
            {
                state.Progress = -state.Progress;
                state.Party = colonizeParty;
                state.Event = ColonizationEvent.Destroyed;
            }
            else
                state.Event = ColonizationEvent.Destroying;
        }
    }
}

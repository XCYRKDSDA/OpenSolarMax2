// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 推进殖民进度的系统
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[
    ReadPrev(typeof(Colonizable)),
    ReadPrev(typeof(AnchoredShipsRegistry)),
    ReadPrev(typeof(ColonizationAbility)),
    Iterate(typeof(ColonizationState))
]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class ProgressColonizationSystem(World world) : ITickSystem
{
    [Query]
    [All<ColonizationState, Colonizable, InTeam.AsAffiliate, AnchoredShipsRegistry>]
    private static void UpdateColonization(
        [Data] GameTime time,
        ref ColonizationState state,
        in Colonizable colonizable,
        in AnchoredShipsRegistry shipsRegistry
    )
    {
        if (shipsRegistry.Ships.Count != 1)
        {
            state.Event = ColonizationEvent.Idle;
            return;
        }

        var colonizeTeam = shipsRegistry.Ships.First().Key;
        var shipsNum = shipsRegistry.Ships.First().Count();

        var deltaProgress =
            shipsNum
            * colonizeTeam.Get<ColonizationAbility>().ProgressPerSecond
            * (float)time.ElapsedGameTime.TotalSeconds;

        if (state.Team == colonizeTeam || state.Team == Entity.Null)
        {
            if (state.Progress >= colonizable.Volume)
                state.Event = ColonizationEvent.Idle;
            else
            {
                state.Team = colonizeTeam;
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
                state.Team = colonizeTeam;
                state.Event = ColonizationEvent.Destroyed;
            }
            else
                state.Event = ColonizationEvent.Destroying;
        }
    }

    public void Update(GameTime gameTime) => UpdateColonizationQuery(world, gameTime);
}

#endif

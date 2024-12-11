using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 推进殖民进度的系统
/// </summary>
[CoreUpdateSystem]
#pragma warning disable CS9113 // 参数未读。
public sealed partial class ProgressColonizationSystem(World world, IAssetsManager assets)
#pragma warning restore CS9113 // 参数未读。
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ColonizationState, InParty.AsAffiliate, AnchoredShipsRegistry>]
    private static void UpdateColonization([Data] GameTime time, ref ColonizationState state,
                                           in AnchoredShipsRegistry shipsRegistry)
    {
        if (shipsRegistry.Ships.Count != 1)
            return;

        var colonizeParty = shipsRegistry.Ships.First().Key;
        var shipsNum = shipsRegistry.Ships.First().Count();

        var deltaProgress = shipsNum * colonizeParty.Entity.Get<ColonizationAbility>().ProgressPerSecond
                                     * (float)time.ElapsedGameTime.TotalSeconds;

        if (state.Party == colonizeParty || colonizeParty == EntityReference.Null)
            state.Progress += deltaProgress;
        else
            state.Progress -= deltaProgress;
    }
}

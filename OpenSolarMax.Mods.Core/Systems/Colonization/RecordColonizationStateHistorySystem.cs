using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[CoreUpdateSystem]
public sealed partial class RecordColonizationStateHistorySystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ColonizationState, ColonizationStateHistory>]
    private static void Record(in ColonizationState now, ref ColonizationStateHistory history)
    {
        history.Previous = now;
    }
}

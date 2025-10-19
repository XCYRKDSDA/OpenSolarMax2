using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
[Iterate(typeof(PlanetAiTimers))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public partial class CountDownPlanetAiTimersSystem(World world) : ITickSystem
{
    [Query]
    [All<PlanetAiTimers>]
    private static void CountDown(ref PlanetAiTimers aiTimers, [Data] GameTime gameTime)
    {
        foreach (var key in aiTimers.TimeLeft.Keys)
            aiTimers.TimeLeft[key] += gameTime.ElapsedGameTime;
    }

    public void Update(GameTime gameTime) => CountDownQuery(world, gameTime);
}

[SimulateSystem, AfterStructuralChanges]
[Iterate(typeof(PlanetAiTimers))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(CountDownPlanetAiTimersSystem))]
public partial class CleanPlanetAiTimerEntry(World world) : ICalcSystem
{
    [Query]
    [All<PlanetAiTimers>]
    private static void CleanEntries(ref PlanetAiTimers aiTimers, [Data] HashSet<Entity> parties)
    {
        var timeLeft = aiTimers.TimeLeft;
        var keysToRemove = timeLeft.Keys.Where(k => !parties.Contains(k)).ToList();
        var keysToAdd = parties.Where(k => !timeLeft.ContainsKey(k)).ToList();
        foreach (var key in keysToRemove) timeLeft.Remove(key);
        foreach (var key in keysToAdd) timeLeft.Add(key, TimeSpan.Zero);
    }

    [Query]
    [All<InParty.AsParty>]
    private static void CountParties(Entity entity, [Data] HashSet<Entity> parties)
    {
        parties.Add(entity);
    }

    public void Update()
    {
        HashSet<Entity> parties = [];
        CountPartiesQuery(world, parties);
        CleanEntriesQuery(world, parties);
    }
}

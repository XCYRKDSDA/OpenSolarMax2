using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Ecs;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.Common.Systems.Timing;

public abstract class CountDownSystemBase<TTimer>(World world) : ITickSystem
    where TTimer : ICountDownTimer
{
    private readonly QueryDescription _timerDesc = new QueryDescription().WithAll<TTimer>();

    public void Update(GameTime t)
    {
        var query = world.Query(in _timerDesc);
        foreach (var chunk in query.GetChunkIterator())
        {
            var recordSpan = chunk.GetSpan<TTimer>();
            foreach (var idx in chunk)
                recordSpan[idx].TimeLeft -= t.ElapsedGameTime;
        }
    }
}

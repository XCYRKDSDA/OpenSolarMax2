using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems.Timing;

public abstract class CountDownSystemBase<TTimer>(World world) : ITickSystem where TTimer : ICountDownTimer
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

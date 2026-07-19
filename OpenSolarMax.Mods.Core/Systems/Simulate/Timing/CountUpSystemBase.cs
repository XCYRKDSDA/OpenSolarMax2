// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems.Timing;

public abstract class CountUpSystemBase<TTimer>(World world) : ITickSystem
    where TTimer : ICountUpTimer
{
    private readonly QueryDescription _timerDesc = new QueryDescription().WithAll<TTimer>();

    public void Update(GameTime t)
    {
        var query = world.Query(in _timerDesc);
        foreach (var chunk in query.GetChunkIterator())
        {
            var recordSpan = chunk.GetSpan<TTimer>();
            foreach (var idx in chunk)
                recordSpan[idx].TimeElapsed += t.ElapsedGameTime;
        }
    }
}

#endif

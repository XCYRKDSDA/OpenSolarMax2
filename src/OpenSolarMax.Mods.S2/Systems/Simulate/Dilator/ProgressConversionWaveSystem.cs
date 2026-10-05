using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 推进转化波的半径：按配置的扩张速度逐帧积分
/// </summary>
[SimulateSystem, Update]
[ReadPrev(typeof(ConversionWaveConfig)), Tick(typeof(ConversionWaveState))]
public sealed partial class ProgressConversionWaveSystem(World world) : ITickSystem
{
    [Query]
    [All<ConversionWaveConfig, ConversionWaveState>]
    private static void Progress(
        in ConversionWaveConfig config,
        ref ConversionWaveState state,
        [Data] GameTime time
    )
    {
        state.Radius += config.ConversionSpeed * (float)time.ElapsedGameTime.TotalSeconds;
    }

    public void Update(GameTime gameTime) => ProgressQuery(world, gameTime);
}

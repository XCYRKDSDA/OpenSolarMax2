using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Systems.Timing;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, Update]
[Tick(typeof(DilatorConversionState))]
public sealed class CountUpDilatorConversionSystem(World world)
    : CountUpSystemBase<DilatorConversionState>(world),
        ITickSystem { }

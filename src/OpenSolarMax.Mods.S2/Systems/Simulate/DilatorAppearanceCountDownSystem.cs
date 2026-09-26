using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.Common.Systems.Timing;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 对临时 dilator 出现事件的阶段时长逐帧倒计时
/// </summary>
[SimulateSystem, Update]
[Tick(typeof(DilatorAppearanceState))]
public sealed class DilatorAppearanceCountDownSystem(World world)
    : CountDownSystemBase<DilatorAppearanceState>(world),
        ITickSystem { }

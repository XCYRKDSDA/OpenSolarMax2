using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Systems.Timing;

namespace OpenSolarMax.Mods.S2.Systems;

[AiSystem, Update]
[Tick(typeof(AiTimer))]
public class AiCountDownSystem(World world) : CountDownSystemBase<AiTimer>(world), ITickSystem { }

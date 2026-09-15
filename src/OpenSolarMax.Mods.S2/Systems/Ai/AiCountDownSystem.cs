using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.Common.Systems.Timing;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

[AiSystem, Update]
[Tick(typeof(AiTimer))]
public class AiCountDownSystem(World world) : CountDownSystemBase<AiTimer>(world), ITickSystem { }

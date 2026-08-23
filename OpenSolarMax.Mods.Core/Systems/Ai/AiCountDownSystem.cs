using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Systems.Timing;

namespace OpenSolarMax.Mods.Core.Systems;

[AiSystem, Update]
[Iterate(typeof(AiTimer))]
public class AiCountDownSystem(World world) : CountDownSystemBase<AiTimer>(world), ITickSystem { }

using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Systems;
using OpenSolarMax.Mods.Core.Systems.Timing;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, Update]
[Tick(typeof(AttackTimer))]
public sealed partial class CooldownAttackTimerSystem(World world)
    : CountDownSystemBase<AttackTimer>(world) { }

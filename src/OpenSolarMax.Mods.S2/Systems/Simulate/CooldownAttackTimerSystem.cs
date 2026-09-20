using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Systems.Timing;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, Update]
[Tick(typeof(AttackTimer))]
public sealed partial class CooldownAttackTimerSystem(World world)
    : CountDownSystemBase<AttackTimer>(world) { }

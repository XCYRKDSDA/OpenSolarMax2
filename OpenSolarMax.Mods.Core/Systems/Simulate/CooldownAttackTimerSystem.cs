using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Systems.Timing;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, Update]
[Iterate(typeof(AttackTimer))]
public sealed partial class CooldownAttackTimerSystem(World world)
    : CountDownSystemBase<AttackTimer>(world) { }

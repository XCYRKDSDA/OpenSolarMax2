using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Systems.Timing;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges, Iterate(typeof(AttackTimer))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class CooldownAttackTimerSystem(World world) : CountDownSystemBase<AttackTimer>(world)
{ }

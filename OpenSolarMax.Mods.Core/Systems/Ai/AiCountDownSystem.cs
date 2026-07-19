// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Core;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Systems.Timing;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
[Iterate(typeof(AiTimer))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public class AiCountDownSystem(World world) : CountDownSystemBase<AiTimer>(world), ITickSystem { }

#endif

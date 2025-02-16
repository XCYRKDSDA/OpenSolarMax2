using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 根据相位计算实体绕其轨道的位姿变换的系统
/// </summary>
[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteBefore(typeof(CalculateAbsoluteTransformSystem))]
public sealed partial class CalculateTransformAroundOrbitSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<TreeRelationship<RelativeTransform>, RelativeTransform, RevolutionOrbit, RevolutionState>]
    private static void CalculateTransform(in RevolutionOrbit orbit, ref RevolutionState state,
                                           ref RelativeTransform transform)
    {
        // 更新相对位姿
        transform.Translation = RevolutionUtils.CalculateTransform(in orbit, in state).Translation;
    }
}

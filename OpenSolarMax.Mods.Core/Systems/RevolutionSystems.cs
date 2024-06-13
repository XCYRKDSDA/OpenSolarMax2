using System.Runtime.CompilerServices;
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
/// 更新公转相位的系统
/// </summary>
/// <param name="world"></param>
/// <param name="assets"></param>
[CoreUpdateSystem]
[ExecuteBefore(typeof(CalculateEntitiesTransformAroundOrbitSystem))]
public sealed partial class UpdateRevolutionPhaseSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<RevolutionOrbit, RevolutionState>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdateRevolution([Data] GameTime time, in RevolutionOrbit orbit, ref RevolutionState state)
    {
        // 更新旋转状态
        state.Phase += MathF.PI * 2 * (float)time.ElapsedGameTime.TotalSeconds / orbit.Period;
    }
}

/// <summary>
/// 根据相位计算实体绕其轨道的位姿变换的系统
/// </summary>
[LateUpdateSystem]
[ExecuteBefore(typeof(AnimateSystem))]
[ExecuteBefore(typeof(CalculateAbsoluteTransformSystem))]
public sealed partial class CalculateEntitiesTransformAroundOrbitSystem(World world, IAssetsManager assets)
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

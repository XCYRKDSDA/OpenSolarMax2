using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 根据相位计算实体绕其轨道的位姿变换的系统
/// </summary>
[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(RevolutionOrbit)), ReadCurr(typeof(RevolutionState))]
[Write(typeof(RelativeTransform))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class CalculateTransformAroundOrbitSystem(World world) : ICalcSystem
{
    [Query]
    [All<TreeRelationship<RelativeTransform>, RelativeTransform, RevolutionOrbit, RevolutionState>]
    private static void CalculateTransform(in RevolutionOrbit orbit, in RevolutionState state,
                                           ref RelativeTransform transform)
    {
        // 更新相对位姿
        transform.Translation = RevolutionUtils.CalculateTransform(in orbit, in state).Translation;
    }

    public void Update() => CalculateTransformQuery(world);
}

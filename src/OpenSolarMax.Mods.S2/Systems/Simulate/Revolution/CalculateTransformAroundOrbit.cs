using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Utils;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 根据相位计算实体绕其轨道的位姿变换的系统
/// </summary>
[SimulateSystem, LateUpdate, BothForGameplayAndPreview]
[
    ReadCurr(typeof(RevolutionOrbit)),
    ReadCurr(typeof(RevolutionState)),
    Calc(typeof(RelativeTransform))
]
[ExecuteAfter(typeof(ApplyAnimationSystem), "默认动画系统优先执行", typeof(RelativeTransform))]
public sealed partial class CalculateTransformAroundOrbitSystem(World world) : ICalcSystem
{
    [Query]
    [All<TreeRelationship<RelativeTransform>, RelativeTransform, RevolutionOrbit, RevolutionState>]
    private static void CalculateTransform(
        in RevolutionOrbit orbit,
        in RevolutionState state,
        ref RelativeTransform transform
    )
    {
        // 该系统同时负责天体绕轨道公转和飞船绕天体公转，而天体绕轨道公转是不应当改变姿态的。
        // 虽然按理说飞船绕天体公转时应当改变姿态，但是游戏中也无处体现。之前以为不更新姿态影响了传送动画，实际上并非如此。
        // 因此此处仍然只设置相对位置变换，不设置姿态。
        // 后续若有需要，应当采用可配置的逻辑：是只更新位置，还是要更新姿态。

        // 更新相对位姿
        transform.Translation = RevolutionUtils.CalculateTransform(in orbit, in state).Translation;
    }

    public void Update() => CalculateTransformQuery(world);
}

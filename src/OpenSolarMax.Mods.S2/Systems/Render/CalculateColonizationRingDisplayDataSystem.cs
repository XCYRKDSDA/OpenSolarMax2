using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 按殖民状态计算天体殖民进度环的显示数据。
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(Colonizable))]
[ReadCurr(typeof(ColonizationState))]
[ReadCurr(typeof(RecommendedVisualStyle))]
[Calc(typeof(ColonizationRingDisplayData))]
public sealed partial class CalculateColonizationRingDisplayDataSystem(World world) : ICalcSystem
{
    [Query]
    [All<Colonizable, ColonizationState, ColonizationRingDisplayData>]
    private static void Calculate(
        in Colonizable colonizable,
        in ColonizationState state,
        ref ColonizationRingDisplayData display
    )
    {
        if (state.Team == Entity.Null)
        {
            display = default;
            return;
        }

        display.Color = state.Team.Get<RecommendedVisualStyle>().Color;
        display.Angle = MathHelper.TwoPi * state.Progress / colonizable.Volume;
    }

    public void Update() => CalculateQuery(world);
}

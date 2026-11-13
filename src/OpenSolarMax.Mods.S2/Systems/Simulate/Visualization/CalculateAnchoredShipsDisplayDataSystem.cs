using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 按天体上停靠舰船的阵营构成，计算构成弧与数目标签的显示数据。
/// </summary>
[LateUpdate]
[SimulateSystem]
[ReadCurr(typeof(AnchoredShipsRegistry))]
[ReadCurr(typeof(RecommendedVisualStyle))]
[ReadCurr(typeof(TeamVisualization))]
[Calc(typeof(AnchoredShipsDisplayData))]
[ExecuteAfter(
    typeof(ApplyAnimationSystem),
    "默认动画系统优先执行",
    typeof(AnchoredShipsDisplayData)
)]
public sealed partial class CalculateAnchoredShipsDisplayDataSystem(
    World world,
    [Section("systems:visualization:anchored_ships")] IConfiguration configs
) : ICalcSystem
{
    private readonly string _textFormat = configs.RequireValue<string>("text:template");
    private readonly float _arcGapAngle = configs.RequireValue<Angle>("ring:gap");

    /// <summary>
    /// 根据权重计算每段弧线的启停角度。
    /// 由于所有弧线逐一相连，故直接返回各个交界处的角度序列
    /// </summary>
    private float[] CalculateArcs(int[] weights)
    {
        float weightsSum = weights.Sum();
        var total = MathHelper.TwoPi - _arcGapAngle * weights.Length;

        var alphas = weights.Select(w => w / weightsSum * total + _arcGapAngle).ToArray();

        var thetas = new float[weights.Length];
        thetas[0] = alphas[0] / 2;
        for (var i = 1; i < weights.Length; i++)
            thetas[i] = thetas[i - 1] + (alphas[i] + alphas[i - 1]) / 2;

        var offset = 0f;
        for (var i = 0; i < weights.Length; i++)
            offset += weights[i] * (MathHelper.TwoPi * i / weights.Length - thetas[i]);
        offset /= weightsSum;

        var arcsAngles = new float[weights.Length + 1];
        arcsAngles[0] = offset - MathHelper.PiOver2;
        for (var i = 0; i < weights.Length; i++)
            arcsAngles[i + 1] = arcsAngles[i] + alphas[i];

        return arcsAngles;
    }

    [Query]
    [All<AnchoredShipsRegistry, AnchoredShipsDisplayData>]
    private void Calculate(in AnchoredShipsRegistry registry, ref AnchoredShipsDisplayData display)
    {
        var ships = registry.Ships;
        if (ships.Count == 0)
        {
            display = new AnchoredShipsDisplayData();
            return;
        }

        var parties = ships.Select(g => g.Key).Order().ToArray();
        var weights = parties.Select(party => ships[party].Count()).ToArray();

        display.Parties = parties;
        display.PartiesSet = [.. parties];
        display.Weights = weights;
        display.Colors = parties
            .Select(party => party.Get<RecommendedVisualStyle>().Color)
            .ToArray();
        display.Texts = weights.Select(weight => string.Format(_textFormat, weight)).ToArray();
        display.LabelledParties = parties
            .Where(party => !party.Get<TeamVisualization>().HideShipCount)
            .ToHashSet();
        display.ArcAngles = CalculateArcs(weights);
    }

    public void Update() => CalculateQuery(world);
}

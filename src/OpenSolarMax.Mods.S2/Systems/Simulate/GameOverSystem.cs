using System.Diagnostics;
using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(Victory)),
    ReadCurr(typeof(InTeam.AsTeam)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(Colonizable)),
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(ReferenceSize)),
    ReadCurr(typeof(ViewTag)),
    ReadCurr(typeof(LevelClearState)),
    DelayedCalc
]
public sealed partial class GameOverSystem(
    World world,
    IConceptFactory factory,
    [Section("systems:victory")] IConfiguration configs
) : IDelayedCalcSystem
{
    private readonly float _waveMaxInterval = configs.GetValue<float>("wave_max_interval");
    private readonly float _waveTotalSeconds = configs.GetValue<float>("wave_total_seconds");

    [Query]
    [All<InTeam.AsTeam, Victory>]
    private static void FindWinner(Entity team, in Victory victory, [Data] ref Entity winner)
    {
        if (!victory.HasWon)
            return;

        Debug.Assert(winner == Entity.Null, "同一帧中出现了多个胜者");
        winner = team;
    }

    [Query]
    [All<ViewTag, LevelClearState>]
    private static void FindSettledViews(in LevelClearState clearState, [Data] ref bool settled)
    {
        if (clearState.Status != ClearStatus.NotCleared)
            settled = true;
    }

    [Query]
    [All<InTeam.AsAffiliate, Colonizable, ColonizationState, AbsoluteTransform, ReferenceSize>]
    private static void FindAllPlanets(
        Entity planet,
        in AbsoluteTransform transform,
        [Data] List<(Entity Planet, Vector2 Pos)> collected
    )
    {
        collected.Add((planet, new Vector2(transform.Translation.X, transform.Translation.Y)));
    }

    private void SpawnVictoryEffects(Entity winner, CommandBuffer commandBuffer)
    {
        var planets = new List<(Entity Planet, Vector2 Pos)>();
        FindAllPlanetsQuery(world, planets);

        // 所有星球（含中立和己方）的 XY 质心
        var centroid = planets.Aggregate(Vector2.Zero, (acc, p) => acc + p.Pos) / planets.Count;

        // 按距质心距离排序（近→远）；距离相近（差 ≤ 容差）时按 atan2 角度升序排（+X 为零，逆时针为正）
        var sorted = planets
            .Select(p =>
            {
                var dx = p.Pos.X - centroid.X;
                var dy = p.Pos.Y - centroid.Y;
                var dist = MathF.Sqrt(dx * dx + dy * dy);
                var angle = MathF.Atan2(dy, dx);
                if (angle < 0)
                    angle += 2 * MathF.PI;
                return (p.Planet, Dist: dist, Angle: angle);
            })
            .OrderBy(p => p.Dist)
            .ThenBy(p => p.Angle)
            .ToList();

        // 不分桶：每颗星球按排序顺序依次触发，rank 即序号
        var ranked = sorted.Select((p, i) => (Rank: i, p.Planet)).ToList();

        // 计算波纹间隔 Δ = min(wave_max_interval, wave_total_seconds / M)
        // 其中 M 为星球总数
        var M = ranked.Count > 0 ? ranked.Count : 1;
        var delta = MathF.Min(_waveMaxInterval, _waveTotalSeconds / MathF.Max(1, M));

        // 为每颗星球创建调度实体，延迟 = rank × Δ 秒
        foreach (var (r, planet) in ranked)
        {
            factory.Make(
                world,
                commandBuffer,
                new PendingVictoryEffectDescription
                {
                    Planet = planet,
                    Winner = winner,
                    TimeLeft = TimeSpan.FromSeconds(r * delta),
                }
            );
        }

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.VictoryExitTimer,
            new VictoryExitTimerDescription { TimeLeft = TimeSpan.FromSeconds(_waveTotalSeconds) }
        );

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.VictoryFlash,
            new VictoryFlashDescription { Team = winner }
        );
    }

    [Query]
    [All<ViewTag, LevelClearState>]
    private static void ClaimViews(
        Entity viewEntity,
        in LevelClearState clearState,
        [Data] CommandBuffer commandBuffer
    )
    {
        if (clearState.Status != ClearStatus.NotCleared)
            return;

        // 经缓冲写入裁决结果，下一轮迭代起对所有系统可见
        commandBuffer.Set(in viewEntity, new LevelClearState { Status = ClearStatus.Cleared });
    }

    public void Update(CommandBuffer commandBuffer)
    {
        // 判断是否出现胜者
        var winner = Entity.Null;
        FindWinnerQuery(world, ref winner);
        if (winner == Entity.Null)
            return;

        // 结局已被其他判定通道裁决时，不再启动默认结束流程
        var settled = false;
        FindSettledViewsQuery(world, ref settled);
        if (settled)
            return;

        // 在所有天体上触发胜利效果
        SpawnVictoryEffects(winner, commandBuffer);

        // 设置所有视图上的通关状态
        ClaimViewsQuery(world, commandBuffer);
    }
}

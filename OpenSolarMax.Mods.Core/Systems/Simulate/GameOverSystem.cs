using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, BeforeStructuralChanges]
// 按理说应当使用 ReadCurr，但是目前不支持 StructuralChange 位于 PostUpdate 后边，因此只能晚一帧生效
[
    ReadPrev(typeof(Victory)),
    ReadPrev(typeof(InTeam.AsTeam)),
    ReadPrev(typeof(InTeam.AsAffiliate)),
    ReadPrev(typeof(Colonizable)),
    ReadPrev(typeof(AbsoluteTransform)),
    ReadPrev(typeof(ReferenceSize)),
    ReadPrev(typeof(VictoryEffectMarker)),
    ChangeStructure
]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class GameOverSystem(
    World world,
    IConceptFactory factory,
    [Section("systems:victory")] IConfiguration configs
) : ICalcSystemWithStructuralChanges
{
    private readonly float _waveMaxInterval = configs.GetValue<float>("wave_max_interval");
    private readonly float _waveTotalSeconds = configs.GetValue<float>("wave_total_seconds");

    [Query]
    [All<InTeam.AsTeam, Victory>]
    private static void FindWinner(Entity team, in Victory victory, [Data] List<Entity> winners)
    {
        if (victory.HasWon)
            winners.Add(team);
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

    public void Update(CommandBuffer commandBuffer)
    {
        var winners = new List<Entity>();
        FindWinnerQuery(world, winners);
        if (winners.Count == 0)
            return;

        var hasEffectMarker = false;
        world.Query(
            new QueryDescription().WithAll<VictoryEffectMarker>(),
            (Entity _) => hasEffectMarker = true
        );
        if (hasEffectMarker)
            return;

        var winner = winners[0];

        // 收集所有星球（含中立和己方），计算 XY 质心
        var planets = new List<(Entity Planet, Vector2 Pos)>();
        FindAllPlanetsQuery(world, planets);

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

        commandBuffer.Create(new Signature(typeof(VictoryEffectMarker)));

        factory.Make(
            world,
            commandBuffer,
            ConceptNames.VictoryExitTimer,
            new VictoryExitTimerDescription { TimeLeft = TimeSpan.FromSeconds(_waveTotalSeconds) }
        );

        var flashColor = winner.Get<TeamReferenceColor>().Value;
        factory.Make(
            world,
            commandBuffer,
            ConceptNames.VictoryFlash,
            new VictoryFlashDescription { Color = flashColor }
        );
    }
}

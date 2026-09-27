using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Configuration;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.Configuration;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;
using OpenSolarMax.Mods.S2.Concepts;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 推进临时 dilator 出现事件的状态机：创建演出、构造 dilator 与舰队、派遣舰队、退场销毁
/// </summary>
[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(DilatorAppearanceState)),
    ReadCurr(typeof(AbsoluteTransform)),
    ReadCurr(typeof(InTeam.AsAffiliate)),
    ReadCurr(typeof(Battlefield)),
    ReadCurr(typeof(Colonizable)),
    ReadCurr(typeof(AnchoredShipsRegistry)),
    ReadCurr(typeof(JumpingShipsRegistry)),
    ReadCurr(typeof(JumpingStatus)),
    ReadCurr(typeof(Jumpable)),
    ReadCurr(typeof(PlanetAiTimers)),
    ReadCurr(typeof(Garrison)),
    ReadCurr(typeof(ProductionCondition)),
    DelayedCalc
]
public sealed partial class ProgressDilatorAppearanceSystem(
    World world,
    IAssetsManager assets,
    IConceptFactory factory,
    [Section("applier:dilator_appearance")] IConfiguration configs
) : IDelayedCalcSystem
{
    private readonly SafeFmodEventDescription _bossAppearSound =
        assets.Load<SafeFmodEventDescription>($"{Content.Sounds.Master_bank}:/BossAppear");

    private readonly SafeFmodEventDescription _bossReadySound =
        assets.Load<SafeFmodEventDescription>($"{Content.Sounds.Master_bank}:/BossReady");

    private readonly SafeFmodEventDescription _bossReverseSound =
        assets.Load<SafeFmodEventDescription>($"{Content.Sounds.Master_bank}:/BossReverse");

    private readonly TimeSpan _standingDuration = TimeSpan.FromSeconds(
        configs.RequireValue<float>("standing_duration")
    );

    private readonly TimeSpan _exitDuration = TimeSpan.FromSeconds(
        configs.RequireValue<float>("exit_duration")
    );

    /// <summary>
    /// 派遣目标。记录距离、预测敌方兵力与容积
    /// </summary>
    public readonly record struct DispatchTarget(
        Entity Entity,
        float Distance,
        int PredictedEnemyShips,
        float Volume
    );

    /// <summary>
    /// 统计某阵营飞行途中（已飞行距离超过 50）的舰船数
    /// </summary>
    private static int CountTravellingShips(IEnumerable<Entity> ships, Entity team)
    {
        var speed = team.Get<Jumpable>().Speed;
        return ships.Count(ship =>
        {
            var status = ship.Get<JumpingStatus>();
            return status.State == JumpingState.Travelling
                && status.Travelling.ElapsedTime * speed > 50f;
        });
    }

    /// <summary>
    /// 与 EnemyAiSystem 一致的敌方兵力预测算法
    /// </summary>
    private static int PredictEnemyShips(
        in AnchoredShipsRegistry anchoredShipsRegistry,
        in JumpingShipsRegistry jumpingShipsRegistry,
        Entity team,
        Entity bodyTeam,
        bool canProduce
    )
    {
        // lambda 内无法捕获 in 参数，转存为局部变量
        var incomingShips = jumpingShipsRegistry.IncomingShips;

        return anchoredShipsRegistry
            .Ships.Where(group => group.Key != team)
            .Select(group =>
            {
                var incoming = CountTravellingShips(incomingShips[group.Key], group.Key);
                var strength = group.Count() + incoming;
                if (canProduce && bodyTeam == group.Key)
                    strength = (int)(strength * 1.25f);
                return strength;
            })
            .DefaultIfEmpty(0)
            .Max();
    }

    [Query]
    [All<
        InTeam.AsAffiliate,
        Battlefield,
        Colonizable,
        AnchoredShipsRegistry,
        JumpingShipsRegistry,
        AbsoluteTransform,
        PlanetAiTimers,
        Garrison
    >]
    private static void CollectDispatchTargets(
        Entity planet,
        in InTeam.AsAffiliate asAffiliate,
        in AnchoredShipsRegistry anchoredShipsRegistry,
        in JumpingShipsRegistry jumpingShipsRegistry,
        in AbsoluteTransform pose,
        in Colonizable colonizable,
        [Data] Entity team,
        [Data] Vector2 origin,
        [Data] List<DispatchTarget> targets
    )
    {
        var bodyTeam = asAffiliate.Relationship is null
            ? Entity.Null
            : asAffiliate.Relationship.Value.Copy.Team;
        var canProduce = planet.Has<ProductionCondition>();

        var predictedEnemyShips = PredictEnemyShips(
            in anchoredShipsRegistry,
            in jumpingShipsRegistry,
            team,
            bodyTeam,
            canProduce
        );

        targets.Add(
            new DispatchTarget(
                planet,
                Vector2.Distance(new Vector2(pose.Translation.X, pose.Translation.Y), origin),
                predictedEnemyShips,
                colonizable.Volume
            )
        );
    }

    /// <summary>
    /// 按距 dilator 由近及远的顺序为候选天体分批派出飞船：
    /// 每批目标飞船数目取「目标预测敌方兵力 × 2」与「目标体积 × 2」的较大者；剩余舰船不足当前预算时并入上一批。
    /// </summary>
    /// <remarks>
    /// 和原版的换算关系如下：原版所有天体数据均由尺寸导出，但本项目中人口数、体积等均为独立数据。
    /// 原版中派出飞船数按照「半径 / 64 × 200」计算，而原版中标准行星满足「体积 = 半径 / 6.4」。
    /// 故此处换算为「目标体积 × 2」
    /// </remarks>
    private void DispatchShips(
        Entity dilator,
        Entity team,
        Vector3 origin,
        int shipCount,
        CommandBuffer commandBuffer
    )
    {
        var targets = new List<DispatchTarget>();
        CollectDispatchTargetsQuery(world, team, new Vector2(origin.X, origin.Y), targets);
        targets.Sort((left, right) => left.Distance.CompareTo(right.Distance));

        var remaining = shipCount;
        var batches = new List<(Entity Destination, int Count)>();
        foreach (var target in targets)
        {
            if (remaining <= 0)
                break;

            var budget = Math.Max(target.PredictedEnemyShips * 2, (int)(target.Volume * 2));

            if (budget <= remaining)
            {
                batches.Add((target.Entity, budget));
                remaining -= budget;
            }
            else
            {
                if (batches.Count > 0)
                {
                    // 剩余不足以填满当前预算，并入上一批
                    var (lastDestination, lastCount) = batches[^1];
                    batches[^1] = (lastDestination, lastCount + remaining);
                }
                else
                {
                    // 本批就是第一批，直接设置
                    batches.Add((target.Entity, remaining));
                }
                remaining = 0;
            }
        }

        // 全部批次同帧发出，不走 AI 系统
        foreach (var (destination, count) in batches)
            factory.Make(
                world,
                commandBuffer,
                new JumpingRequestDescription
                {
                    Departure = dilator,
                    Destination = destination,
                    Team = team,
                    ExpectedNum = count,
                }
            );
    }

    /// <summary>
    /// 初始阶段：创建入场演出并播放入场音效，把阶段推进到入场演出
    /// </summary>
    private void StartAppearing(
        Entity entity,
        in DilatorAppearanceState state,
        in AbsoluteTransform pose,
        CommandBuffer commandBuffer
    )
    {
        factory.Make(
            world,
            commandBuffer,
            new DarkPulseEntryDescription { Position = pose.Translation, Team = state.Team }
        );

        factory.Make(
            world,
            commandBuffer,
            new SimpleSoundDescription()
            {
                Transform = new AbsoluteTransformOptions() { Translation = pose.Translation },
                SoundEffect = _bossAppearSound,
            }
        );

        commandBuffer.Set(entity, state with { Phase = DilatorAppearancePhase.Appearing });
    }

    /// <summary>
    /// 入场演出结束：构造 dilator 与舰队并立即全部派出，播放中场演出与音效，把阶段推进到亮相
    /// </summary>
    private void SpawnDilatorAndDispatch(
        Entity entity,
        in DilatorAppearanceState state,
        in AbsoluteTransform pose,
        CommandBuffer commandBuffer
    )
    {
        if (state.TimeLeft > TimeSpan.Zero)
            return;

        var dilator = factory.Make(
            world,
            commandBuffer,
            ConceptNames.TemporaryDilator,
            new TemporaryDilatorDescription
            {
                Transform = new AbsoluteTransformOptions
                {
                    Translation = pose.Translation,
                    Rotation = pose.Rotation,
                },
                Team = state.Team,
                ShipCount = state.ShipCount,
            }
        );
        DispatchShips(dilator, state.Team, pose.Translation, state.ShipCount, commandBuffer);

        factory.Make(
            world,
            commandBuffer,
            new DarkPulseReadyDescription { Position = pose.Translation, Team = state.Team }
        );
        factory.Make(
            world,
            commandBuffer,
            new SimpleSoundDescription()
            {
                Transform = new AbsoluteTransformOptions() { Translation = pose.Translation },
                SoundEffect = _bossReadySound,
            }
        );

        commandBuffer.Set(
            entity,
            state with
            {
                Phase = DilatorAppearancePhase.Standing,
                TimeLeft = _standingDuration,
                Dilator = dilator,
            }
        );
    }

    /// <summary>
    /// 亮相结束：创建退场演出并播放退场音效，把阶段推进到退场
    /// </summary>
    private void StartExiting(
        Entity entity,
        in DilatorAppearanceState state,
        in AbsoluteTransform pose,
        CommandBuffer commandBuffer
    )
    {
        if (state.TimeLeft > TimeSpan.Zero)
            return;

        factory.Make(
            world,
            commandBuffer,
            new DarkPulseExitDescription { Position = pose.Translation, Team = state.Team }
        );

        factory.Make(
            world,
            commandBuffer,
            new SimpleSoundDescription()
            {
                Transform = new AbsoluteTransformOptions() { Translation = pose.Translation },
                SoundEffect = _bossReverseSound,
            }
        );

        commandBuffer.Set(
            entity,
            state with
            {
                Phase = DilatorAppearancePhase.Exiting,
                TimeLeft = _exitDuration,
            }
        );
    }

    /// <summary>
    /// 退场演出结束：销毁 dilator，事件实体随即自毁
    /// </summary>
    private static void FinishAppearance(
        Entity entity,
        in DilatorAppearanceState state,
        CommandBuffer commandBuffer
    )
    {
        if (state.TimeLeft > TimeSpan.Zero)
            return;

        if (state.Dilator != Entity.Null && state.Dilator.IsAlive())
            commandBuffer.Destroy(state.Dilator);
        commandBuffer.Destroy(entity);
    }

    [Query]
    [All<DilatorAppearanceState, AbsoluteTransform>]
    private void Progress(
        Entity entity,
        in DilatorAppearanceState state,
        in AbsoluteTransform pose,
        [Data] CommandBuffer commandBuffer
    )
    {
        switch (state.Phase)
        {
            case DilatorAppearancePhase.Initial:
                StartAppearing(entity, in state, in pose, commandBuffer);
                break;
            case DilatorAppearancePhase.Appearing:
                SpawnDilatorAndDispatch(entity, in state, in pose, commandBuffer);
                break;
            case DilatorAppearancePhase.Standing:
                StartExiting(entity, in state, in pose, commandBuffer);
                break;
            case DilatorAppearancePhase.Exiting:
                FinishAppearance(entity, in state, commandBuffer);
                break;
        }
    }

    public void Update(CommandBuffer commandBuffer) => ProgressQuery(world, commandBuffer);
}

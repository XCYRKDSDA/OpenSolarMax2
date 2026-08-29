using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;

namespace OpenSolarMax.Mods.Core.Systems;

[AiSystem, LateUpdate]
[ReadCurr(typeof(InTeam.AsAffiliate))]
[ReadCurr(typeof(InTeam.AsTeam))]
[ReadCurr(typeof(Battlefield))]
[ReadCurr(typeof(ProductionCondition))]
[ReadCurr(typeof(Colonizable))]
[ReadCurr(typeof(AnchoredShipsRegistry))]
[ReadCurr(typeof(JumpingShipsRegistry))]
[ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(Ai))]
[ReadCurr(typeof(TeamPopulationRegistry))]
[ReadCurr(typeof(ReachabilityRegistry))]
[Consume(typeof(AiTimer))]
[Consume(typeof(PlanetAiTimers))]
[ChangeStructure]
public partial class SimpleEnemyAiSystem(World world, IConceptFactory factory)
    : ICalcSystemWithStructuralChanges
{
    private static readonly Random Random = new();

    public struct PlanetInfo
    {
        public Entity Entity;

        public TimeSpan AiTimeLeft;

        public Entity Team;

        public Vector2 Position;
        public float Volume;

        public int ActualFriendShips;
        public int PredictedFriendShips;

        public int ActualEnemyShips;
        public int PredictedEnemyShips;

        public bool Battle;
        public bool CanProduce;
    }

    [Query]
    [All<
        InTeam.AsAffiliate,
        Battlefield,
        ProductionCondition,
        Colonizable,
        AnchoredShipsRegistry,
        JumpingShipsRegistry,
        AbsoluteTransform,
        PlanetAiTimers
    >]
    private static void CollectPlanetInfo(
        Entity planet,
        in InTeam.AsAffiliate asAffiliate,
        in Battlefield battlefield,
        in ProductionCondition productionCondition,
        in Colonizable colonizable,
        in AnchoredShipsRegistry anchoredShipsRegistry,
        in JumpingShipsRegistry jumpingShipsRegistry,
        in AbsoluteTransform absoluteTransform,
        in PlanetAiTimers planetAiTimers,
        [Data] Entity team,
        [Data] Dictionary<Entity, PlanetInfo> planetInfos
    )
    {
        // 在途舰队注册表（lambda 内无法捕获 in 参数，转存为局部变量）
        var incomingShips = jumpingShipsRegistry.IncomingShips;

        planetInfos.Add(
            planet,
            new PlanetInfo()
            {
                Entity = planet,
                AiTimeLeft = planetAiTimers.TimeLeft[team],
                Team = asAffiliate.Relationship is null
                    ? Entity.Null
                    : asAffiliate.Relationship.Value.Copy.Team,
                Position =
                {
                    X = absoluteTransform.Translation.X,
                    Y = absoluteTransform.Translation.Y,
                },
                Volume = colonizable.Volume,
                ActualFriendShips = anchoredShipsRegistry.Ships[team].Count(),
                PredictedFriendShips =
                    anchoredShipsRegistry.Ships[team].Count()
                    + jumpingShipsRegistry.IncomingShips[team].Count(),
                ActualEnemyShips = anchoredShipsRegistry
                    .Ships.Where(g => g.Key != team)
                    .Select(g => g.Count())
                    .DefaultIfEmpty(0)
                    .Max(),
                PredictedEnemyShips = anchoredShipsRegistry
                    .Ships.Where(g => g.Key != team)
                    .Select(g => g.Count() + incomingShips[g.Key].Count())
                    .DefaultIfEmpty(0)
                    .Max(),
                Battle = battlefield.FrontlineDamage.Count > 0,
                CanProduce = productionCondition.IsMet,
            }
        );
    }

    private static bool CheckBlocked(PlanetInfo departure, PlanetInfo destination) =>
        !departure.Entity.Get<ReachabilityRegistry>().FromHereTo[destination.Entity];

    [Query]
    [All<Ai, InTeam.AsTeam, AiTimer>]
    private void Execute(
        Entity team,
        in Ai ai,
        ref AiTimer timer,
        [Data] CommandBuffer commandBuffer
    )
    {
        // lambda 内无法捕获 in 参数，值拷贝后供查询表达式使用
        var aiParams = ai;
        if (timer.TimeLeft > TimeSpan.Zero)
            return;
        var jitterFactor =
            ai.JitterMinFactor + Random.NextDouble() * (ai.JitterMaxFactor - ai.JitterMinFactor);
        timer.TimeLeft = TimeSpan.FromSeconds(ai.ActionIntervalSeconds * jitterFactor);

        // 统计星球信息
        var planetInfos = new Dictionary<Entity, PlanetInfo>();
        CollectPlanetInfoQuery(world, team, planetInfos);

        // 启用挂机检查且上限为 0、总飞船数低于阈值时挂机
        ref readonly var populationRegistry = ref team.Get<TeamPopulationRegistry>();
        if (
            ai.IdleCheckEnabled
            && populationRegistry is { PopulationLimit: 0 }
            && populationRegistry.CurrentPopulation < ai.IdlePopulationThreshold
        )
            return;

        // 计算己方天体中心
        var friendPlanets = planetInfos.Values.Where(info => info.Team == team).ToList();
        if (friendPlanets.Count == 0)
            return; // 己方无天体时无法计算中心，跳过本次决策
        var friendPlanetsCenter =
            friendPlanets.Select(info => info.Position).Aggregate(Vector2.Zero, (v1, v2) => v1 + v2)
            / friendPlanets.Count;

        if (ai.DefenseEnabled)
        {
            #region 防御

            // 寻找目标防守星球
            var defendTargets = planetInfos
                .Values.Where(info =>
                {
                    // 条件1：为己方天体或有己方飞船（包括飞行中的）
                    if (info.Team != team && info.PredictedFriendShips == 0)
                        return false;
                    // 条件2：有敌方（RequiresEnemy 时启用）
                    if (aiParams.Defense.RequiresEnemy && info.PredictedEnemyShips == 0)
                        return false;
                    // 条件3：排除己方强度高于敌方 × EnemyCoefficient 的安全天体（己方足够强，无需防守）
                    if (
                        info.PredictedFriendShips
                        > info.PredictedEnemyShips * aiParams.Defense.EnemyCoefficient
                    )
                        return false;
                    return true;
                })
                .OrderBy(info =>
                {
                    // 该天体到己方天体几何中心的距离（带随机抖动）
                    var distance =
                        Vector2.Distance(info.Position, friendPlanetsCenter)
                        + Random.NextDouble() * aiParams.Defense.DistanceJitter;
                    // 己方势力强度减去非己方势力强度
                    var relativeStrength = info.PredictedFriendShips - info.PredictedEnemyShips;
                    // 计算防守价值
                    return distance + relativeStrength;
                })
                .ToList();

            // 寻找可出兵防御的天体
            var defendSenders = planetInfos
                .Values.Where(info =>
                {
                    // 基本条件：该天体己方ai倒计时为0且该天体己方强度不为0
                    if (info.AiTimeLeft > TimeSpan.Zero || info.PredictedFriendShips <= 0)
                        return false;
                    // 出兵来源准入：false 时未在战斗才允许派兵，战斗中且己方占优则排除；true 时无对抗威胁（含在途敌船）才允许派兵
                    if (!aiParams.Defense.ConsiderIncomingEnemies)
                    {
                        if (info.Battle && info.PredictedFriendShips > info.PredictedEnemyShips)
                            return false;
                    }
                    else
                    {
                        // 条件：是己方天体或预测己方强度低于敌方
                        if (
                            info.Team != team
                            && info.PredictedFriendShips > info.PredictedEnemyShips
                        )
                            return false;
                        // 条件：没有敌方或预测己方强度低于敌方
                        if (
                            info.PredictedEnemyShips > 0
                            && info.PredictedFriendShips > info.PredictedEnemyShips
                        )
                            return false;
                    }
                    return true;
                })
                .OrderBy(info =>
                {
                    // 将该天体己方强度记为飞船数的相反数
                    return -info.ActualFriendShips;
                })
                .ToList();

            foreach (var target in defendTargets)
            {
                foreach (var sender in defendSenders)
                {
                    // 基本条件：出兵天体和目标天体不为同一个，且二者之间没有被拦截
                    if (sender.Entity == target.Entity || CheckBlocked(sender, target))
                        continue;
                    // 出兵条件：出兵天体和目标天体的己方综合强度须达到目标预测敌方强度（StrengthComparison）
                    var combined = sender.ActualFriendShips + target.PredictedFriendShips;
                    var pass =
                        ai.Defense.StrengthComparison == AiStrengthComparison.StrictGreater
                            ? combined > target.PredictedEnemyShips
                            : combined >= target.PredictedEnemyShips;
                    if (!pass)
                        continue;

                    // 飞船数：目标预测敌方强度 × EnemyCoefficient − 预测己方强度 × AllyCoefficient
                    var shipsToSend = (int)(
                        target.PredictedEnemyShips * ai.Defense.EnemyCoefficient
                        - target.PredictedFriendShips * ai.Defense.AllyCoefficient
                    );

                    // TODO: 估损（DamageEstimateCoefficient 占位，等 Tower 实装后按 getLengthInTowerRange/4.5 实现）
                    var towerAttack = 0;
                    shipsToSend += towerAttack; // 为飞船数加上估损

                    // 条件：没有经过攻击天体或总兵力多于估损
                    if (towerAttack > 0 && populationRegistry.CurrentPopulation < towerAttack)
                        continue;
                    // 条件：没有经过攻击天体或出兵天体强度高于估损的一半
                    if (towerAttack > 0 && sender.ActualFriendShips < towerAttack / 2)
                        continue;
                    // 飞船数为零或负值时跳过该组合，继续尝试后续组合
                    if (shipsToSend <= 0)
                        continue;

                    // 创建舰船移动请求
                    factory.Make(
                        world,
                        commandBuffer,
                        new JumpingRequestDescription()
                        {
                            Departure = sender.Entity,
                            Destination = target.Entity,
                            Team = team,
                            ExpectedNum = shipsToSend,
                        }
                    );
                    sender.Entity.Get<PlanetAiTimers>().TimeLeft[team] = TimeSpan.FromSeconds(
                        ai.PlanetCooldownSeconds
                    ); // TODO 随机化

                    return;
                }
            }

            #endregion
        }

        if (ai.AttackEnabled)
        {
            #region 进攻

            // 寻找可进攻的天体
            var attackTargets = planetInfos
                .Values.Where(info =>
                {
                    // 基本条件：不为己方天体
                    if (info.Team == team)
                        return false;
                    // 条件：排除已占据且兵力充足的天体（ExclusionScopeNeutralOnly 时仅限中立）
                    if (
                        info.PredictedEnemyShips == 0
                        && info.PredictedFriendShips
                            > info.Volume * aiParams.Attack.ExclusionThresholdMultiplier
                        && (
                            aiParams.Attack.ExclusionScopeNeutralOnly
                                ? info.Team == Entity.Null
                                : true
                        )
                    )
                        return false;
                    // 条件：敌方不足己方一半不打（ExcludeWeakEnemy）
                    if (
                        aiParams.Attack.ExcludeWeakEnemy
                        && info.PredictedEnemyShips > 0
                        && info.PredictedFriendShips * 0.5 > info.PredictedEnemyShips
                    )
                        return false;
                    return true;
                })
                .OrderBy(info =>
                {
                    // 该天体到己方天体几何中心的距离（带随机抖动）
                    var distance =
                        Vector2.Distance(info.Position, friendPlanetsCenter)
                        + Random.NextDouble() * aiParams.Attack.DistanceJitter;
                    // 预测敌方强度减去预测己方强度
                    var relativeStrength = info.PredictedEnemyShips - info.PredictedFriendShips;
                    // 计算进攻价值
                    return distance + relativeStrength;
                })
                .ToList();

            // 寻找可出兵进攻的天体
            var attackSenders = planetInfos
                .Values.Where(info =>
                {
                    // 基本条件：该天体己方ai倒计时为0且该天体己方强度不为0
                    if (info.AiTimeLeft > TimeSpan.Zero || info.PredictedFriendShips <= 0)
                        return false;
                    // 条件：排除锁星中的天体（ExcludeCapturingSenders）
                    if (
                        aiParams.Attack.ExcludeCapturingSenders
                        && info.PredictedEnemyShips == 0
                        && info.Team != team
                    )
                        return false;
                    // 出兵来源准入：false 时未在战斗才允许派兵，战斗中且己方占优则排除；true 时无对抗威胁（含在途敌船）才允许派兵
                    if (!aiParams.Attack.ConsiderIncomingEnemies)
                    {
                        if (info.Battle && info.PredictedFriendShips > info.PredictedEnemyShips)
                            return false;
                    }
                    else
                    {
                        // 条件：是己方天体或预测己方强度低于敌方
                        if (
                            info.Team != team
                            && info.PredictedFriendShips > info.PredictedEnemyShips
                        )
                            return false;
                        // 条件：没有敌方或预测己方强度低于敌方
                        if (
                            info.PredictedEnemyShips > 0
                            && info.PredictedFriendShips > info.PredictedEnemyShips
                        )
                            return false;
                    }
                    return true;
                })
                .OrderBy(info =>
                {
                    // 将该天体己方强度记为飞船数的相反数
                    return -info.ActualFriendShips;
                })
                .ToList();

            foreach (var target in attackTargets)
            {
                foreach (var sender in attackSenders)
                {
                    // 基本条件：出兵天体和目标天体不为同一个，且二者之间没有被拦截
                    if (sender.Entity == target.Entity || CheckBlocked(sender, target))
                        continue;
                    // 出兵条件：出兵天体和目标天体的己方综合强度须达到目标预测敌方强度（StrengthComparison）
                    var combined = sender.ActualFriendShips + target.PredictedFriendShips;
                    var pass =
                        ai.Attack.StrengthComparison == AiStrengthComparison.StrictGreater
                            ? combined > target.PredictedEnemyShips
                            : combined >= target.PredictedEnemyShips;
                    if (!pass)
                        continue;

                    // 基本飞船数：目标预测敌方强度 × EnemyCoefficient − 预测己方强度 × AllyCoefficient
                    var shipsToSend = (int)(
                        target.PredictedEnemyShips * ai.Attack.EnemyCoefficient
                        - target.PredictedFriendShips * ai.Attack.AllyCoefficient
                    );
                    // 出兵天体受威胁（预测敌方强度大于己方）时倾巢
                    var threatened = sender.PredictedEnemyShips > sender.PredictedFriendShips;
                    if (ai.Attack.AllOutPriority == AiAllOutPriority.AllOutFirst)
                    {
                        // S2 Simple：先下限后倾巢，倾巢覆盖下限
                        if (shipsToSend < target.Volume * ai.Attack.LowerBoundMultiplier)
                            shipsToSend = (int)(target.Volume * ai.Attack.LowerBoundMultiplier);
                        if (threatened)
                            shipsToSend = sender.ActualFriendShips;
                    }
                    else // LowerBoundFirst：S2 Smart/Dark 先倾巢后下限，下限覆盖倾巢
                    {
                        if (threatened)
                            shipsToSend = sender.ActualFriendShips;
                        if (shipsToSend < target.Volume * ai.Attack.LowerBoundMultiplier)
                            shipsToSend = (int)(target.Volume * ai.Attack.LowerBoundMultiplier);
                    }

                    // TODO: 估损（DamageEstimateCoefficient 占位，等 Tower 实装后按 getLengthInTowerRange/4.5 实现）
                    var towerAttack = 0;
                    shipsToSend += towerAttack; // 为飞船数加上估损

                    // 总兵力不足估损时不派兵
                    if (towerAttack > 0 && populationRegistry.CurrentPopulation < towerAttack)
                        continue;
                    // 出兵天体强度低于估损的一半时不派兵
                    if (towerAttack > 0 && sender.ActualFriendShips < towerAttack / 2)
                        continue;
                    // 飞船数为零或负值时跳过该组合，继续尝试后续组合
                    if (shipsToSend <= 0)
                        continue;

                    // 创建舰船移动请求
                    factory.Make(
                        world,
                        commandBuffer,
                        new JumpingRequestDescription()
                        {
                            Departure = sender.Entity,
                            Destination = target.Entity,
                            Team = team,
                            ExpectedNum = shipsToSend,
                        }
                    );
                    sender.Entity.Get<PlanetAiTimers>().TimeLeft[team] = TimeSpan.FromSeconds(
                        ai.PlanetCooldownSeconds
                    );
                    return;
                }
            }

            #endregion
        }

        if (ai.GatherEnabled)
        {
            #region 聚兵

            var aiValues = planetInfos.ToDictionary(
                p => p.Key,
                pair =>
                {
                    ref readonly var reachabilityRegistry =
                        ref pair.Key.Get<ReachabilityRegistry>();
                    var value = reachabilityRegistry
                        .FromHereTo.Where(p => p.Value)
                        .Count(p =>
                            planetInfos[p.Key].Team != team
                            || planetInfos[p.Key].PredictedEnemyShips > 0
                        );
                    return value;
                }
            );
            // TODO: 传送门价值加成（WarpValueBonus 参数占位，等 Warp 实装后按 S2 语义减 1）

            var gatherSender = planetInfos
                .Values.Where(info =>
                {
                    // 条件：仅己方出兵来源（OwnTeamSendersOnly）
                    if (aiParams.Gather.OwnTeamSendersOnly && info.Team != team)
                        return false;
                    // 条件：没在锁星（非仅己方出兵来源时启用）
                    if (
                        !aiParams.Gather.OwnTeamSendersOnly
                        && info is { PredictedEnemyShips: 0, ActualFriendShips: > 0 }
                    )
                        return false;
                    // 出兵来源准入：false 时未在战斗才允许派兵，战斗中则排除；true 时无对抗威胁（含在途敌船）才允许派兵
                    if (!aiParams.Gather.ConsiderIncomingEnemies)
                    {
                        if (info.Battle)
                            return false;
                    }
                    else
                    {
                        // 条件：无敌方或打不过敌方
                        if (
                            info.PredictedEnemyShips > 0
                            && info.PredictedFriendShips > info.PredictedEnemyShips
                        )
                            return false;
                    }
                    return true;
                })
                .OrderBy(info =>
                {
                    // 己方强度（SortAddsStrongestEnemy 时叠加最强敌方驻留）的相反数
                    var friendStrength = info.ActualFriendShips;
                    var strongestEnemy = aiParams.Gather.SortAddsStrongestEnemy
                        ? info.ActualEnemyShips
                        : 0;
                    return -(friendStrength + strongestEnemy);
                })
                .ToList();

            foreach (var target in planetInfos.Values)
            {
                foreach (var sender in gatherSender)
                {
                    // 基本条件：出兵天体和目标天体不为同一个，且二者之间没有被拦截
                    if (sender.Entity == target.Entity || CheckBlocked(sender, target))
                        continue;
                    // 条件：目标天体价值高于出兵天体价值
                    if (aiValues[target.Entity] >= aiValues[sender.Entity])
                        continue;

                    // 派出全部飞船
                    var shipsToSend = sender.ActualFriendShips;

                    // TODO: 估损（DamageEstimateCoefficient 占位，等 Tower 实装后按 getLengthInTowerRange/4.5 实现）
                    var towerAttack = 0;
                    shipsToSend += towerAttack; // 为飞船数加上估损

                    // 总兵力不足估损时不派兵
                    if (towerAttack > 0 && populationRegistry.CurrentPopulation < towerAttack)
                        continue;
                    // 出兵天体强度低于估损的一半时不派兵
                    if (towerAttack > 0 && sender.ActualFriendShips < towerAttack / 2)
                        continue;
                    // 飞船数为零或负值时跳过该组合，继续尝试后续组合
                    if (shipsToSend <= 0)
                        continue;

                    // 创建舰船移动请求
                    factory.Make(
                        world,
                        commandBuffer,
                        new JumpingRequestDescription()
                        {
                            Departure = sender.Entity,
                            Destination = target.Entity,
                            Team = team,
                            ExpectedNum = shipsToSend,
                        }
                    );
                    sender.Entity.Get<PlanetAiTimers>().TimeLeft[team] = TimeSpan.FromSeconds(
                        ai.PlanetCooldownSeconds
                    ); // TODO 随机化
                    return;
                }
            }

            #endregion
        }
    }

    public void Update(CommandBuffer commandBuffer) => ExecuteQuery(world, commandBuffer);
}

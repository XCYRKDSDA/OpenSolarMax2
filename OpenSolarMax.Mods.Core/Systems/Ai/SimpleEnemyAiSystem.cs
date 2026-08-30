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
[ReadCurr(typeof(Colonizable))]
[ReadCurr(typeof(AnchoredShipsRegistry))]
[ReadCurr(typeof(JumpingShipsRegistry))]
[ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(Ai))]
[ReadCurr(typeof(TeamPopulationRegistry))]
[ReadCurr(typeof(ReachabilityRegistry))]
[ReadCurr(typeof(AiValueBonus))]
[ReadCurr(typeof(ProductionCondition))]
[ReadCurr(typeof(JumpingStatus))]
[ReadCurr(typeof(Jumpable))]
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

    #region 共享工具

    private static bool CheckBlocked(PlanetInfo departure, PlanetInfo destination) =>
        !departure.Entity.Get<ReachabilityRegistry>().FromHereTo[destination.Entity];

    /// <summary>
    /// 出兵来源准入：未在战斗才允许派兵。防御/进攻段战斗中且己方占优则排除（抽兵会失守），聚兵段战斗中即排除。
    /// </summary>
    private static bool IsSenderAdmissible(
        PlanetInfo info,
        Entity team,
        bool considerIncomingEnemies,
        bool requireBattleSuperiority
    )
    {
        if (!considerIncomingEnemies)
        {
            if (
                info.Battle
                && (
                    !requireBattleSuperiority
                    || info.PredictedFriendShips > info.PredictedEnemyShips
                )
            )
                return false;
            return true;
        }
        // 条件：是己方天体或预测己方强度低于敌方
        if (
            requireBattleSuperiority
            && info.Team != team
            && info.PredictedFriendShips > info.PredictedEnemyShips
        )
            return false;
        // 条件：没有敌方或预测己方强度低于敌方
        if (info.PredictedEnemyShips > 0 && info.PredictedFriendShips > info.PredictedEnemyShips)
            return false;
        return true;
    }

    /// <summary>
    /// 出兵条件：己方综合兵力是否大于目标预测敌方兵力（AllowEqual 时大于等于亦可）。
    /// </summary>
    private static bool IsStrengthGreater(int combined, int enemyStrength, bool allowEqual) =>
        allowEqual ? combined >= enemyStrength : combined > enemyStrength;

    /// <summary>
    /// 派兵数：目标预测敌方强度 × 敌方兵力系数 − 目标预测己方强度 × 己方兵力系数。
    /// </summary>
    private static int CalculateShipsToSend(
        int predictedEnemy,
        int predictedFriend,
        float enemyCoefficient,
        float allyCoefficient
    ) => (int)(predictedEnemy * enemyCoefficient - predictedFriend * allyCoefficient);

    /// <summary>
    /// 目标到己方天体几何中心的距离（带随机抖动）。
    /// </summary>
    private static double CalculateDistanceWithJitter(
        Vector2 position,
        Vector2 center,
        float jitter
    ) => Vector2.Distance(position, center) + Random.NextDouble() * jitter;

    /// <summary>
    /// 出兵来源排序键：-(己方兵力 + (叠加最强敌方 ? 最强敌方驻留兵力 : 0))，值越小越先派兵。
    /// </summary>
    private static int CalculateSenderOrderKey(PlanetInfo info, bool addsStrongestEnemy) =>
        -(info.ActualFriendShips + (addsStrongestEnemy ? info.ActualEnemyShips : 0));

    /// <summary>
    /// 估算派兵航线上的路上损耗（占位，等 Tower 实装后按 getLengthInTowerRange/4.5 实现）。
    /// </summary>
    private static int EstimateRouteDamage(in PlanetInfo sender, in PlanetInfo target)
    {
        // TODO: 估损（DamageEstimateCoefficient 占位，等 Tower 实装后按 getLengthInTowerRange/4.5 实现）
        return 0;
    }

    /// <summary>
    /// 已知路上损耗后，评估是否允许派兵：总兵力不足损耗或出兵天体兵力不足损耗一半时不允许。
    /// </summary>
    private static bool IsDispatchAllowedGivenDamage(
        int routeDamage,
        in PlanetInfo sender,
        in TeamPopulationRegistry populationRegistry
    )
    {
        // 总兵力不足估损时不派兵
        if (routeDamage > 0 && populationRegistry.CurrentPopulation < routeDamage)
            return false;
        // 出兵天体强度低于估损的一半时不派兵
        if (routeDamage > 0 && sender.ActualFriendShips < routeDamage / 2)
            return false;
        return true;
    }

    /// <summary>
    /// 创建舰船移动请求并记录出兵来源冷却。
    /// </summary>
    private void SendShips(
        CommandBuffer commandBuffer,
        Entity team,
        in PlanetInfo sender,
        in PlanetInfo target,
        int shipsToSend,
        float planetCooldownSeconds
    )
    {
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
            planetCooldownSeconds
        ); // TODO 随机化
    }

    #endregion

    #region 数据收集

    [Query]
    [All<
        InTeam.AsAffiliate,
        Battlefield,
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

        var bodyTeam = asAffiliate.Relationship is null
            ? Entity.Null
            : asAffiliate.Relationship.Value.Copy.Team;
        var canProduce = planet.Has<ProductionCondition>();

        // 己方在途含 Charging（充能中）舰船，S2 仅计飞行中（state==3）；
        // 本游戏跳跃系统比 S2 多一个充能阶段，此处口径略宽，属有意保留的已知偏差，影响极小
        var friendStrength =
            anchoredShipsRegistry.Ships[team].Count() + incomingShips[team].Count();
        if (canProduce && bodyTeam == team)
            friendStrength = (int)(friendStrength * 1.25f);

        planetInfos.Add(
            planet,
            new PlanetInfo()
            {
                Entity = planet,
                AiTimeLeft = planetAiTimers.TimeLeft[team],
                Team = bodyTeam,
                Position =
                {
                    X = absoluteTransform.Translation.X,
                    Y = absoluteTransform.Translation.Y,
                },
                Volume = colonizable.Volume,
                ActualFriendShips = anchoredShipsRegistry.Ships[team].Count(),
                PredictedFriendShips = friendStrength,
                ActualEnemyShips = anchoredShipsRegistry
                    .Ships.Where(g => g.Key != team)
                    .Select(g => g.Count())
                    .DefaultIfEmpty(0)
                    .Max(),
                PredictedEnemyShips = anchoredShipsRegistry
                    .Ships.Where(g => g.Key != team)
                    .Select(g =>
                    {
                        var enemyIncoming = incomingShips[g.Key]
                            .Count(ship =>
                                ship.Get<JumpingStatus>().State == JumpingState.Travelling
                                && ship.Get<JumpingStatus>().Travelling.ElapsedTime
                                    * g.Key.Get<Jumpable>().Speed
                                    > 50f
                            );
                        var s = g.Count() + enemyIncoming;
                        if (canProduce && bodyTeam == g.Key)
                            s = (int)(s * 1.25f);
                        return s;
                    })
                    .DefaultIfEmpty(0)
                    .Max(),
                Battle = battlefield.FrontlineDamage.Count > 0,
                CanProduce = canProduce,
            }
        );
    }

    #endregion

    #region 防御

    /// <summary>
    /// 寻找目标防守星球：按防守价值排序。
    /// </summary>
    private static List<PlanetInfo> ExtractDefendTargets(
        Dictionary<Entity, PlanetInfo> planetInfos,
        Entity team,
        Vector2 friendPlanetsCenter,
        AiDefenseParameters defense
    )
    {
        return planetInfos
            .Values.Where(info =>
            {
                // 条件1：为己方天体或有己方飞船（包括飞行中的）
                if (info.Team != team && info.PredictedFriendShips == 0)
                    return false;
                // 条件2：有敌方（RequiresEnemy 时启用）
                if (defense.RequiresEnemy && info.PredictedEnemyShips == 0)
                    return false;
                // 条件3：排除己方强度高于敌方 × 敌方兵力系数的安全天体（己方足够强，无需防守）
                if (info.PredictedFriendShips > info.PredictedEnemyShips * defense.EnemyCoefficient)
                    return false;
                return true;
            })
            .OrderBy(info =>
                CalculateDistanceWithJitter(
                    info.Position,
                    friendPlanetsCenter,
                    defense.DistanceJitter
                ) + (info.PredictedFriendShips - info.PredictedEnemyShips)
            )
            .ToList();
    }

    /// <summary>
    /// 寻找可出兵防御的天体：按己方兵力从多到少排序。
    /// </summary>
    private static List<PlanetInfo> ExtractDefendSenders(
        Dictionary<Entity, PlanetInfo> planetInfos,
        Entity team,
        AiDefenseParameters defense
    )
    {
        return planetInfos
            .Values.Where(info =>
            {
                // 基本条件：该天体己方 AI 冷却为 0 且该天体己方强度不为 0
                if (info.AiTimeLeft > TimeSpan.Zero || info.ActualFriendShips <= 0)
                    return false;
                // 出兵来源准入：未在战斗才允许派兵，战斗中且己方占优则排除（抽兵会失守）
                return IsSenderAdmissible(
                    info,
                    team,
                    defense.ConsiderIncomingEnemies,
                    requireBattleSuperiority: true
                );
            })
            .OrderBy(info => CalculateSenderOrderKey(info, addsStrongestEnemy: false))
            .ToList();
    }

    /// <summary>
    /// 防御：寻找目标防守星球并尝试派兵，派兵成功返回 true。
    /// </summary>
    private bool TryDispatchDefense(
        Entity team,
        in Ai ai,
        Dictionary<Entity, PlanetInfo> planetInfos,
        Vector2 friendPlanetsCenter,
        in TeamPopulationRegistry populationRegistry,
        CommandBuffer commandBuffer
    )
    {
        // 寻找目标防守星球
        var defendTargets = ExtractDefendTargets(
            planetInfos,
            team,
            friendPlanetsCenter,
            ai.Defense
        );
        // 寻找可出兵防御的天体
        var defendSenders = ExtractDefendSenders(planetInfos, team, ai.Defense);

        foreach (var target in defendTargets)
        {
            foreach (var sender in defendSenders)
            {
                // 出兵天体和目标天体不为同一个，且二者之间没有被拦截
                if (sender.Entity == target.Entity || CheckBlocked(sender, target))
                    continue;
                // 出兵天体和目标天体的己方综合兵力须达到目标预测敌方兵力
                var combined = sender.ActualFriendShips + target.PredictedFriendShips;
                if (!IsStrengthGreater(combined, target.PredictedEnemyShips, ai.Defense.AllowEqual))
                    continue;

                // 飞船数：目标预测敌方强度 × 敌方兵力系数 − 目标预测己方强度 × 己方兵力系数
                var shipsToSend = CalculateShipsToSend(
                    target.PredictedEnemyShips,
                    target.PredictedFriendShips,
                    ai.Defense.EnemyCoefficient,
                    ai.Defense.AllyCoefficient
                );
                // 加上路上损耗，损耗过大时放弃
                var routeDamage = EstimateRouteDamage(in sender, in target);
                shipsToSend += routeDamage;
                if (!IsDispatchAllowedGivenDamage(routeDamage, in sender, in populationRegistry))
                    continue;
                // 飞船数为零或负值时跳过该组合，继续尝试后续组合
                if (shipsToSend <= 0)
                    continue;

                // 创建舰船移动请求并记录出兵冷却
                SendShips(
                    commandBuffer,
                    team,
                    in sender,
                    in target,
                    shipsToSend,
                    ai.PlanetCooldownSeconds
                );
                return true;
            }
        }
        return false;
    }

    #endregion

    #region 进攻

    /// <summary>
    /// 寻找可进攻的天体：按进攻价值排序。
    /// </summary>
    private static List<PlanetInfo> ExtractAttackTargets(
        Dictionary<Entity, PlanetInfo> planetInfos,
        Entity team,
        Vector2 friendPlanetsCenter,
        AiAttackParameters attack
    )
    {
        return planetInfos
            .Values.Where(info =>
            {
                // 基本条件：不为己方天体
                if (info.Team == team)
                    return false;
                // 条件：排除已占据且兵力充足的天体（ExclusionScopeNeutralOnly 时仅限中立）
                if (
                    info.PredictedEnemyShips == 0
                    && info.PredictedFriendShips > info.Volume * attack.ExclusionThresholdMultiplier
                    && (attack.ExclusionScopeNeutralOnly ? info.Team == Entity.Null : true)
                )
                    return false;
                // 条件：敌方不足己方一半不打（ExcludeWeakEnemy）
                if (
                    attack.ExcludeWeakEnemy
                    && info.PredictedEnemyShips > 0
                    && info.PredictedFriendShips * 0.5 > info.PredictedEnemyShips
                )
                    return false;
                return true;
            })
            .OrderBy(info =>
                CalculateDistanceWithJitter(
                    info.Position,
                    friendPlanetsCenter,
                    attack.DistanceJitter
                ) + (info.PredictedEnemyShips - info.PredictedFriendShips)
            )
            .ToList();
    }

    /// <summary>
    /// 寻找可出兵进攻的天体：按己方兵力从多到少排序。
    /// </summary>
    private static List<PlanetInfo> ExtractAttackSenders(
        Dictionary<Entity, PlanetInfo> planetInfos,
        Entity team,
        AiAttackParameters attack
    )
    {
        return planetInfos
            .Values.Where(info =>
            {
                // 基本条件：该天体己方 AI 冷却为 0 且该天体己方强度不为 0
                if (info.AiTimeLeft > TimeSpan.Zero || info.ActualFriendShips <= 0)
                    return false;
                // 条件：排除锁星中的天体（ExcludeCapturingSenders）
                if (
                    attack.ExcludeCapturingSenders
                    && info.PredictedEnemyShips == 0
                    && info.Team != team
                )
                    return false;
                // 出兵来源准入：未在战斗才允许派兵，战斗中且己方占优则排除（抽兵会失守）
                return IsSenderAdmissible(
                    info,
                    team,
                    attack.ConsiderIncomingEnemies,
                    requireBattleSuperiority: true
                );
            })
            .OrderBy(info => CalculateSenderOrderKey(info, addsStrongestEnemy: false))
            .ToList();
    }

    /// <summary>
    /// 进攻：出兵来源受威胁时决定是否派出全部兵力。
    /// </summary>
    private static int ApplyAllOutPriority(
        int shipsToSend,
        in PlanetInfo sender,
        in PlanetInfo target,
        AiAttackParameters attack
    )
    {
        // 出兵来源受威胁（预测敌方强度大于己方）时派出全部兵力
        var threatened = sender.PredictedEnemyShips > sender.PredictedFriendShips;
        if (attack.AllOutPriority == AiAllOutPriority.AllOutFirst)
        {
            // 先按下限派兵，受威胁时改为派出全部兵力（全出覆盖下限）
            if (shipsToSend < target.Volume * attack.LowerBoundMultiplier)
                shipsToSend = (int)(target.Volume * attack.LowerBoundMultiplier);
            if (threatened)
                shipsToSend = sender.ActualFriendShips;
        }
        else // LowerBoundFirst：先判断全出再按下限保证（下限覆盖全出）
        {
            if (threatened)
                shipsToSend = sender.ActualFriendShips;
            if (shipsToSend < target.Volume * attack.LowerBoundMultiplier)
                shipsToSend = (int)(target.Volume * attack.LowerBoundMultiplier);
        }
        return shipsToSend;
    }

    /// <summary>
    /// 进攻：寻找可进攻的天体并尝试派兵，派兵成功返回 true。
    /// </summary>
    private bool TryDispatchAttack(
        Entity team,
        in Ai ai,
        Dictionary<Entity, PlanetInfo> planetInfos,
        Vector2 friendPlanetsCenter,
        in TeamPopulationRegistry populationRegistry,
        CommandBuffer commandBuffer
    )
    {
        // 寻找可进攻的天体
        var attackTargets = ExtractAttackTargets(planetInfos, team, friendPlanetsCenter, ai.Attack);
        // 寻找可出兵进攻的天体
        var attackSenders = ExtractAttackSenders(planetInfos, team, ai.Attack);

        foreach (var target in attackTargets)
        {
            foreach (var sender in attackSenders)
            {
                // 出兵天体和目标天体不为同一个，且二者之间没有被拦截
                if (sender.Entity == target.Entity || CheckBlocked(sender, target))
                    continue;
                // 出兵天体和目标天体的己方综合兵力须达到目标预测敌方兵力
                var combined = sender.ActualFriendShips + target.PredictedFriendShips;
                if (!IsStrengthGreater(combined, target.PredictedEnemyShips, ai.Attack.AllowEqual))
                    continue;

                // 基本飞船数：目标预测敌方强度 × 敌方兵力系数 − 目标预测己方强度 × 己方兵力系数
                var shipsToSend = CalculateShipsToSend(
                    target.PredictedEnemyShips,
                    target.PredictedFriendShips,
                    ai.Attack.EnemyCoefficient,
                    ai.Attack.AllyCoefficient
                );
                // 出兵来源受威胁时决定是否派出全部兵力
                shipsToSend = ApplyAllOutPriority(shipsToSend, in sender, in target, ai.Attack);
                // 加上路上损耗，损耗过大时放弃
                var routeDamage = EstimateRouteDamage(in sender, in target);
                shipsToSend += routeDamage;
                if (!IsDispatchAllowedGivenDamage(routeDamage, in sender, in populationRegistry))
                    continue;
                // 飞船数为零或负值时跳过该组合，继续尝试后续组合
                if (shipsToSend <= 0)
                    continue;

                // 创建舰船移动请求并记录出兵冷却
                SendShips(
                    commandBuffer,
                    team,
                    in sender,
                    in target,
                    shipsToSend,
                    ai.PlanetCooldownSeconds
                );
                return true;
            }
        }
        return false;
    }

    #endregion

    #region 聚兵

    /// <summary>
    /// 计算各天体的聚兵价值：可达目标中非己方或有敌情的数量的负数（传送门目标再减价值加成），值越小越优先作为目标。
    /// </summary>
    private static Dictionary<Entity, int> CalculateGatherValues(
        Dictionary<Entity, PlanetInfo> planetInfos,
        Entity team
    )
    {
        return planetInfos.ToDictionary(
            pair => pair.Key,
            pair =>
            {
                ref readonly var reachabilityRegistry = ref pair.Key.Get<ReachabilityRegistry>();
                var value = -reachabilityRegistry
                    .FromHereTo.Where(entry => entry.Value)
                    .Count(entry =>
                        planetInfos[entry.Key].Team != team
                        || planetInfos[entry.Key].PredictedEnemyShips > 0
                    );
                if (pair.Key.Has<AiValueBonus>())
                    value -= pair.Key.Get<AiValueBonus>().Value;
                return value;
            }
        );
    }

    /// <summary>
    /// 寻找可出兵聚兵的天体：按己方兵力（可叠加最强敌方）从多到少排序。
    /// </summary>
    private static List<PlanetInfo> ExtractGatherSenders(
        Dictionary<Entity, PlanetInfo> planetInfos,
        Entity team,
        AiGatherParameters gather
    )
    {
        return planetInfos
            .Values.Where(info =>
            {
                // 条件：仅己方出兵来源（OwnTeamSendersOnly）
                if (gather.OwnTeamSendersOnly && info.Team != team)
                    return false;
                // 条件：没在锁星（非仅己方出兵来源时启用）
                if (
                    !gather.OwnTeamSendersOnly
                    && info.Team != team
                    && info is { PredictedEnemyShips: 0, ActualFriendShips: > 0 }
                )
                    return false;
                // 出兵来源准入：未在战斗才允许派兵，战斗中则排除
                return IsSenderAdmissible(
                    info,
                    team,
                    gather.ConsiderIncomingEnemies,
                    requireBattleSuperiority: false
                );
            })
            .OrderBy(info => CalculateSenderOrderKey(info, gather.SortAddsStrongestEnemy))
            .ToList();
    }

    /// <summary>
    /// 聚兵：计算各天体价值并尝试派兵，派兵成功返回 true。
    /// </summary>
    private bool TryDispatchGather(
        Entity team,
        in Ai ai,
        Dictionary<Entity, PlanetInfo> planetInfos,
        in TeamPopulationRegistry populationRegistry,
        CommandBuffer commandBuffer
    )
    {
        // 计算各天体的聚兵价值
        var gatherValues = CalculateGatherValues(planetInfos, team);
        // 寻找可出兵聚兵的天体
        var gatherSenders = ExtractGatherSenders(planetInfos, team, ai.Gather);
        // 聚兵目标按价值升序排序
        var gatherTargets = planetInfos.Values.OrderBy(t => gatherValues[t.Entity]).ToList();
        foreach (var target in gatherTargets)
        {
            foreach (var sender in gatherSenders)
            {
                // 出兵天体和目标天体不为同一个，且二者之间没有被拦截
                if (sender.Entity == target.Entity || CheckBlocked(sender, target))
                    continue;
                // 目标天体价值高于出兵天体价值才派兵
                if (gatherValues[target.Entity] >= gatherValues[sender.Entity])
                    continue;

                // 派出全部飞船
                var shipsToSend = sender.ActualFriendShips;
                // 加上路上损耗，损耗过大时放弃
                var routeDamage = EstimateRouteDamage(in sender, in target);
                shipsToSend += routeDamage;
                if (!IsDispatchAllowedGivenDamage(routeDamage, in sender, in populationRegistry))
                    continue;
                // 飞船数为零或负值时跳过该组合，继续尝试后续组合
                if (shipsToSend <= 0)
                    continue;

                // 创建舰船移动请求并记录出兵冷却
                SendShips(
                    commandBuffer,
                    team,
                    in sender,
                    in target,
                    shipsToSend,
                    ai.PlanetCooldownSeconds
                );
                return true;
            }
        }
        return false;
    }

    #endregion

    #region 决策编排

    /// <summary>
    /// 决策节奏：冷却未到返回 false，否则按预设节奏重置冷却并返回 true。
    /// </summary>
    private static bool TryAdvanceTimer(in Ai ai, ref AiTimer timer)
    {
        if (timer.TimeLeft > TimeSpan.Zero)
            return false;
        var jitterFactor =
            ai.JitterMinFactor + Random.NextDouble() * (ai.JitterMaxFactor - ai.JitterMinFactor);
        timer.TimeLeft = TimeSpan.FromSeconds(ai.ActionIntervalSeconds * jitterFactor);
        return true;
    }

    /// <summary>
    /// 挂机检查：人口上限为 0 且总兵力低于阈值时挂机。
    /// </summary>
    private static bool CheckIdle(in Ai ai, in TeamPopulationRegistry populationRegistry) =>
        ai.IdleCheckEnabled
        && populationRegistry is { PopulationLimit: 0 }
        && populationRegistry.CurrentPopulation < ai.IdlePopulationThreshold;

    /// <summary>
    /// 计算己方天体几何中心；己方无天体时返回 false。
    /// </summary>
    private static bool TryComputeFriendPlanetsCenter(
        Dictionary<Entity, PlanetInfo> planetInfos,
        Entity team,
        out Vector2 center
    )
    {
        var friendPlanets = planetInfos.Values.Where(info => info.Team == team).ToList();
        if (friendPlanets.Count == 0)
        {
            center = default;
            return false;
        }
        center =
            friendPlanets.Select(info => info.Position).Aggregate(Vector2.Zero, (v1, v2) => v1 + v2)
            / friendPlanets.Count;
        return true;
    }

    [Query]
    [All<Ai, InTeam.AsTeam, AiTimer>]
    private void Execute(
        Entity team,
        in Ai ai,
        ref AiTimer timer,
        [Data] CommandBuffer commandBuffer
    )
    {
        // 决策节奏：冷却未到则跳过本次决策
        if (!TryAdvanceTimer(in ai, ref timer))
            return;

        // 统计星球信息
        var planetInfos = new Dictionary<Entity, PlanetInfo>();
        CollectPlanetInfoQuery(world, team, planetInfos);

        // 挂机检查：启用挂机且人口上限为 0、总飞船数低于阈值时挂机
        ref readonly var populationRegistry = ref team.Get<TeamPopulationRegistry>();
        if (CheckIdle(in ai, in populationRegistry))
            return;

        // 计算己方天体几何中心；己方无天体时无法决策
        if (!TryComputeFriendPlanetsCenter(planetInfos, team, out var friendPlanetsCenter))
            return;

        // 依次尝试防御、进攻、聚兵，任一阶段派出舰队即结束本轮
        if (
            ai.DefenseEnabled
            && TryDispatchDefense(
                team,
                in ai,
                planetInfos,
                friendPlanetsCenter,
                in populationRegistry,
                commandBuffer
            )
        )
            return;
        if (
            ai.AttackEnabled
            && TryDispatchAttack(
                team,
                in ai,
                planetInfos,
                friendPlanetsCenter,
                in populationRegistry,
                commandBuffer
            )
        )
            return;
        if (
            ai.GatherEnabled
            && TryDispatchGather(team, in ai, planetInfos, in populationRegistry, commandBuffer)
        )
            return;
    }

    #endregion

    public void Update(CommandBuffer commandBuffer) => ExecuteQuery(world, commandBuffer);
}

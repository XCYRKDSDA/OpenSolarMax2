using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Systems;

[AiSystem, BeforeStructuralChanges, ChangeStructure]
// 不记录另一个域的组件读写
public partial class SimpleEnemyAiSystem(World world) : ICalcSystemWithStructuralChanges
{
    public struct PlanetInfo
    {
        public Entity Entity;

        public TimeSpan AiTimeLeft;

        public Entity Party;

        public Vector2 Position;
        public float Volume;

        public int ActualFriendUnits;
        public int PredictedFriendUnits;

        public int ActualEnemyUnits;
        public int PredictedEnemyUnits;

        public bool Battle;
        public bool CanProduce;
    }

    [Query]
    [All<InParty.AsAffiliate, Battlefield, ProductionCondition, Colonizable, AnchoredShipsRegistry,
        ShippingUnitsRegistry, AbsoluteTransform, PlanetAiTimers>]
    private static void CollectPlanetInfo(Entity planet, in InParty.AsAffiliate asAffiliate, in Battlefield battlefield,
                                          in ProductionCondition productionCondition, in Colonizable colonizable,
                                          in AnchoredShipsRegistry anchoredShipsRegistry,
                                          in ShippingUnitsRegistry shippingUnitsRegistry,
                                          in AbsoluteTransform absoluteTransform, in PlanetAiTimers planetAiTimers,
                                          [Data] Entity party, [Data] Dictionary<Entity, PlanetInfo> planetInfos)
    {
        planetInfos.Add(planet, new PlanetInfo()
        {
            Entity = planet,
            AiTimeLeft = planetAiTimers.TimeLeft[party],
            Party = asAffiliate.Relationship is null ? Entity.Null : asAffiliate.Relationship.Value.Copy.Party,
            Position = { X = absoluteTransform.Translation.X, Y = absoluteTransform.Translation.Y },
            Volume = colonizable.Volume,
            ActualFriendUnits = anchoredShipsRegistry.Ships[party].Count(),
            PredictedFriendUnits = anchoredShipsRegistry.Ships[party].Count() +
                                   shippingUnitsRegistry.IncomingUnits[party].Count(),
            ActualEnemyUnits = anchoredShipsRegistry.Ships.Where(g => g.Key != party).Sum(g => g.Count()),
            PredictedEnemyUnits = anchoredShipsRegistry.Ships.Where(g => g.Key != party).Sum(g => g.Count()) +
                                  shippingUnitsRegistry.IncomingUnits.Where(g => g.Key != party).Sum(g => g.Count()),
            Battle = battlefield.FrontlineDamage.Count > 0,
            CanProduce = productionCondition.IsMet
        });
    }

    private static bool CheckBlocked(PlanetInfo departure, PlanetInfo destination)
        => !departure.Entity.Get<ReachabilityRegistry>().FromHereTo[destination.Entity];

    [Query]
    [All<Ai, InParty.AsParty, AiCooldown, AiTimer>]
    private void Execute(Entity party, in Ai ai, in AiCooldown cooldown, ref AiTimer timer,
                         [Data] CommandBuffer commandBuffer)
    {
        if (!ai.Enabled) return;

        if (timer.TimeLeft > TimeSpan.Zero) return;
        timer.TimeLeft += cooldown.Duration * (1 + new Random().NextDouble());

        // 统计星球信息
        var planetInfos = new Dictionary<Entity, PlanetInfo>();
        CollectPlanetInfoQuery(world, party, planetInfos);

        // 上限为 0 且总飞船数少于 40 时挂机
        ref readonly var populationRegistry = ref party.Get<PartyPopulationRegistry>();
        if (populationRegistry is { PopulationLimit: 0, CurrentPopulation: < 40 }) return;

        // 计算己方天体中心
        var friendPlanets = planetInfos.Values.Where(info => info.Party == party).ToList();
        var friendPlanetsCenter = friendPlanets.Select(info => info.Position)
                                               .Aggregate(Vector2.Zero, (v1, v2) => v1 + v2)
                                  / friendPlanets.Count;

        #region 防御

        // 寻找目标防守星球
        var defendTargets = planetInfos.Values.Where(info =>
        {
            // 条件1：为己方天体或有己方飞船（包括飞行中的）
            if (info.Party != party && info.PredictedFriendUnits == 0) return false;
            // 条件2：有敌方
            if (info.PredictedEnemyUnits == 0) return false;
            // 条件3：预测己方强度低于敌方两倍（即可能打不过敌方
            if (info.PredictedFriendUnits > info.PredictedEnemyUnits * 2) return false;
            return true;
        }).OrderBy(info =>
        {
            // 该天体到己方天体几何中心的距离
            var distance = Vector2.Distance(info.Position, friendPlanetsCenter); // TODO: 归一化; TODO: 随机化
            // 己方势力强度减去非己方势力强度
            var relativeStrength = info.PredictedFriendUnits - info.PredictedEnemyUnits;
            // 计算防守价值
            return distance + relativeStrength;
        }).ToList();

        // 寻找可出兵防御的天体
        var defendSenders = planetInfos.Values.Where(info =>
        {
            // 基本条件：该天体己方ai倒计时为0且该天体己方强度不为0
            if ( /*info.AiTimeLeft > TimeSpan.Zero ||*/ info.PredictedFriendUnits <= 0) return false;
            // 条件：是己方天体或预测己方强度低于敌方
            if (info.Party != party && info.PredictedFriendUnits > info.PredictedEnemyUnits) return false;
            // 条件：没有敌方或预测己方强度低于敌方
            if (info.PredictedEnemyUnits > 0 && info.PredictedFriendUnits > info.PredictedEnemyUnits) return false;
            return true;
        }).OrderBy(info =>
            {
                // 将该天体己方强度记为飞船数的相反数
                return -info.ActualFriendUnits;
            }
        ).ToList();

        foreach (var target in defendTargets)
        {
            foreach (var sender in defendSenders)
            {
                // 基本条件：出兵天体和目标天体不为同一个，且二者之间没有被拦截
                if (sender.Entity == target.Entity || CheckBlocked(sender, target)) continue;
                // 出兵条件：出兵天体的强度和目标天体的预测强度之和高于目标天体的预测敌方强度
                if (sender.ActualFriendUnits + target.PredictedFriendUnits <= target.PredictedEnemyUnits) continue;

                // 飞船数：目标天体上预测敌方强度的二倍减去预测己方强度
                var unitsToSend = target.PredictedEnemyUnits * 2 - target.PredictedFriendUnits;

                // TODO: 估损
                var towerAttack = 0;
                unitsToSend += towerAttack; // 为飞船数加上估损

                // 条件：没有经过攻击天体或总兵力多于估损
                if (towerAttack > 0 && populationRegistry.CurrentPopulation < towerAttack) continue;
                // 条件：没有经过攻击天体或出兵天体强度高于估损的一半
                if (towerAttack > 0 && sender.ActualFriendUnits < towerAttack / 2) continue;

                // 创建单位移动请求
                world.Make(commandBuffer, new ShippingRequestTemplate()
                {
                    Departure = sender.Entity,
                    Destination = target.Entity,
                    Party = party,
                    ExpectedNum = unitsToSend
                });
                sender.Entity.Get<PlanetAiTimers>().TimeLeft[party] = TimeSpan.FromSeconds(1); // TODO 随机化

                return;
            }
        }

        #endregion

        #region 进攻

        // 寻找可进攻的天体
        var attackTargets = planetInfos.Values.Where(info =>
        {
            // 基本条件：不为己方天体
            if (info.Party == party) return false;
            // 条件：排除己方强度足够且无敌方的天体
            if (info.PredictedEnemyUnits == 0 && info.PredictedFriendUnits > info.Volume) return false;
            return true;
        }).OrderBy(info =>
        {
            // 该天体到己方天体几何中心的距离
            var distance = Vector2.Distance(info.Position, friendPlanetsCenter); // TODO: 归一化; TODO: 随机化
            // 预测敌方强度减去预测己方强度
            var relativeStrength = info.PredictedEnemyUnits - info.PredictedFriendUnits;
            // 计算防守价值
            return distance + relativeStrength;
        }).ToList();

        // 寻找可出兵进攻的天体
        var attackSenders = planetInfos.Values.Where(info =>
        {
            // 基本条件：该天体己方ai倒计时为0且该天体己方强度不为0
            if ( /*info.AiTimeLeft > TimeSpan.Zero ||*/ info.PredictedFriendUnits <= 0) return false;
            // 条件：天体不被己方占据
            if (info.PredictedEnemyUnits == 0 && info.Party != party) return false;
            // 条件：是己方天体或预测己方强度低于敌方
            if (info.Party != party && info.PredictedFriendUnits > info.PredictedEnemyUnits) return false;
            // 条件：没有敌方或预测己方强度低于敌方
            if (info.PredictedEnemyUnits > 0 && info.PredictedFriendUnits > info.PredictedEnemyUnits) return false;
            return true;
        }).OrderBy(info =>
        {
            // 将该天体己方强度记为飞船数的相反数
            return -info.ActualFriendUnits;
        }).ToList();

        foreach (var target in attackTargets)
        {
            foreach (var sender in attackSenders)
            {
                // 基本条件：出兵天体和目标天体不为同一个，且二者之间没有被拦截
                if (sender.Entity == target.Entity || CheckBlocked(sender, target)) continue;
                // 出兵条件：出兵天体和目标天体的己方综合强度高于目标天体的预测敌方强度
                if (sender.ActualFriendUnits + target.PredictedFriendUnits <= target.PredictedEnemyUnits) continue;

                // 基本飞船数：目标天体上预测敌方强度的二倍减去预测己方强度一半
                var unitsToSend = target.PredictedEnemyUnits * 2 - target.PredictedFriendUnits / 2;

                // 预测敌方强度大于己方时，派出全部飞船
                if (sender.PredictedEnemyUnits > sender.PredictedFriendUnits) unitsToSend = sender.ActualFriendUnits;
                // 飞船数不应低于目标的二倍标准兵力
                if (unitsToSend < target.Volume * 2) unitsToSend = (int)(target.Volume * 2);

                // TODO: 估损
                var towerAttack = 0;
                unitsToSend += towerAttack; // 为飞船数加上估损

                // 总兵力不足估损时不派兵
                if (towerAttack > 0 && populationRegistry.CurrentPopulation < towerAttack) continue;
                // 出兵天体强度低于估损的一半时不派兵
                if (towerAttack > 0 && sender.ActualFriendUnits < towerAttack / 2) continue;

                // 创建单位移动请求
                world.Make(commandBuffer, new ShippingRequestTemplate()
                {
                    Departure = sender.Entity,
                    Destination = target.Entity,
                    Party = party,
                    ExpectedNum = unitsToSend
                });
                sender.Entity.Get<PlanetAiTimers>().TimeLeft[party] = TimeSpan.FromSeconds(1);
                return;
            }
        }

        #endregion

        #region 聚兵

        var aiValues = planetInfos.ToDictionary(p => p.Key, pair =>
        {
            ref readonly var reachabilityRegistry = ref pair.Key.Get<ReachabilityRegistry>();
            var value = reachabilityRegistry.FromHereTo
                                            .Where(p => p.Value)
                                            .Count(p => planetInfos[p.Key].Party != party
                                                        || planetInfos[p.Key].PredictedEnemyUnits > 0);
            return value;
        });

        var gatherSender = planetInfos.Values.Where(info =>
        {
            // 条件：没在锁星
            if (info.Party != party && info is { PredictedEnemyUnits: 0, ActualFriendUnits: > 0 }) return false;
            // 条件：无敌方或打不过敌方
            if (info.PredictedEnemyUnits > 0 && info.PredictedFriendUnits > info.PredictedEnemyUnits) return false;
            return true;
        }).OrderBy(info =>
        {
            // 将该天体己方强度记为飞船数的相反数
            return -info.ActualFriendUnits;
        }).ToList();

        foreach (var target in planetInfos.Values)
        {
            foreach (var sender in gatherSender)
            {
                // 基本条件：出兵天体和目标天体不为同一个，且二者之间没有被拦截
                if (sender.Entity == target.Entity || CheckBlocked(sender, target)) continue;
                // 条件：目标天体价值高于出兵天体价值
                if (aiValues[target.Entity] >= aiValues[sender.Entity]) continue;

                // 派出全部飞船
                var unitsToSend = sender.ActualFriendUnits;

                // TODO: 估损
                var towerAttack = 0;
                unitsToSend += towerAttack; // 为飞船数加上估损

                // 总兵力不足估损时不派兵
                if (towerAttack > 0 && populationRegistry.CurrentPopulation < towerAttack) continue;
                // 出兵天体强度低于估损的一半时不派兵
                if (towerAttack > 0 && sender.ActualFriendUnits < towerAttack / 2) continue;

                // 创建单位移动请求
                world.Make(commandBuffer, new ShippingRequestTemplate()
                {
                    Departure = sender.Entity,
                    Destination = target.Entity,
                    Party = party,
                    ExpectedNum = unitsToSend
                });
                sender.Entity.Get<PlanetAiTimers>().TimeLeft[party] = TimeSpan.FromSeconds(1); // TODO 随机化
                return;
            }
        }

        #endregion
    }

    public void Update(CommandBuffer commandBuffer) => ExecuteQuery(world, commandBuffer);
}

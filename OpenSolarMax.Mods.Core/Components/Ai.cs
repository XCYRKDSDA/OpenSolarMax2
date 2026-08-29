namespace OpenSolarMax.Mods.Core.Components;

/// 出兵来源受威胁时派兵数量的决定顺序
public enum AiAllOutPriority
{
    /// 先按下限派兵，出兵来源受威胁时改为派出全部兵力（全出覆盖下限）
    AllOutFirst,

    /// 先判断是否派出全部兵力，再按下限保证出兵数（下限覆盖全出）
    LowerBoundFirst,
}

/// 防御参数
public struct AiDefenseParameters
{
    /// 目标须有敌方兵力才考虑防守
    public bool RequiresEnemy;

    /// 目标按距离排序时叠加的随机抖动（世界距离），值越大目标选择越分散
    public float DistanceJitter;

    /// true：出兵来源无对抗威胁（敌船驻留或在途都算）才允许派兵；false：出兵来源未在战斗才允许派兵
    public bool ConsiderIncomingEnemies;

    /// true：出兵来源与目标的己方综合兵力大于等于目标预测敌方兵力即可；false：须严格大于
    public bool AllowEqual;

    /// 敌方兵力系数：派兵数 = 目标预测敌方兵力 × 敌方兵力系数 − 目标预测己方兵力 × 己方兵力系数
    public float EnemyCoefficient;

    /// 己方兵力系数：派兵数 = 目标预测敌方兵力 × 敌方兵力系数 − 目标预测己方兵力 × 己方兵力系数
    public float AllyCoefficient;

    /// 估损系数（占位，等 Tower 实装后启用）
    public float DamageEstimateCoefficient;
}

/// 进攻参数
public struct AiAttackParameters
{
    /// 目标预测己方兵力须小于目标体积 × 可攻兵力阈值系数才视为可攻
    public float ExclusionThresholdMultiplier;

    /// 可攻目标的排除范围是否仅限中立天体
    public bool ExclusionScopeNeutralOnly;

    /// 目标敌方兵力不足己方一半时不进攻
    public bool ExcludeWeakEnemy;

    /// 目标按距离排序时叠加的随机抖动（世界距离），值越大目标选择越分散
    public float DistanceJitter;

    /// 出兵来源若正在锁星（殖民中）则排除
    public bool ExcludeCapturingSenders;

    /// true：出兵来源无对抗威胁（敌船驻留或在途都算）才允许派兵；false：出兵来源未在战斗才允许派兵
    public bool ConsiderIncomingEnemies;

    /// true：出兵来源与目标的己方综合兵力大于等于目标预测敌方兵力即可；false：须严格大于
    public bool AllowEqual;

    /// 敌方兵力系数：派兵数 = 目标预测敌方兵力 × 敌方兵力系数 − 目标预测己方兵力 × 己方兵力系数
    public float EnemyCoefficient;

    /// 己方兵力系数：派兵数 = 目标预测敌方兵力 × 敌方兵力系数 − 目标预测己方兵力 × 己方兵力系数
    public float AllyCoefficient;

    /// 出兵下限系数：派兵数下限 = 目标体积 × 出兵下限系数
    public float LowerBoundMultiplier;

    /// 出兵来源受威胁时派兵数量的决定顺序
    public AiAllOutPriority AllOutPriority;

    /// 估损系数（占位，等 Tower 实装后启用）
    public float DamageEstimateCoefficient;
}

/// 聚兵参数
public struct AiGatherParameters
{
    /// 出兵来源是否仅限己方天体
    public bool OwnTeamSendersOnly;

    /// 出兵来源按己方兵力排序时是否叠加最强敌方的兵力
    public bool SortAddsStrongestEnemy;

    /// true：出兵来源无对抗威胁（敌船驻留或在途都算）才允许派兵；false：出兵来源未在战斗才允许派兵
    public bool ConsiderIncomingEnemies;

    /// 传送门对目标价值的加成（占位，等 Warp 实装后启用）
    public float WarpValueBonus;

    /// 估损系数（占位，等 Tower 实装后启用）
    public float DamageEstimateCoefficient;
}

/// AI 参数
public struct Ai
{
    /// 两次决策之间的基准间隔（秒），实际间隔 = 基准 × [抖动下限, 抖动上限)
    public float ActionIntervalSeconds;

    /// 部署后首次决策前的等待时间（秒）
    public float InitialDelaySeconds;

    /// 抖动下限系数：实际间隔 = 基准 × [抖动下限, 抖动上限)
    public float JitterMinFactor;

    /// 抖动上限系数：实际间隔 = 基准 × [抖动下限, 抖动上限)
    public float JitterMaxFactor;

    /// 是否启用挂机：人口上限为 0 且当前兵力低于阈值时停止决策
    public bool IdleCheckEnabled;

    /// 挂机判定所用的兵力阈值
    public int IdlePopulationThreshold;

    /// 出兵来源派兵后进入冷却的时长（秒），冷却期内不再从此派兵
    public float PlanetCooldownSeconds;

    /// 是否执行防御决策
    public bool DefenseEnabled;

    /// 是否执行进攻决策
    public bool AttackEnabled;

    /// 是否执行聚兵决策
    public bool GatherEnabled;

    /// 防御决策参数
    public AiDefenseParameters Defense;

    /// 进攻决策参数
    public AiAttackParameters Attack;

    /// 聚兵决策参数
    public AiGatherParameters Gather;

    /// 预设：仅防御与进攻，节奏较慢
    public static readonly Ai Simple = new()
    {
        ActionIntervalSeconds = 3,
        InitialDelaySeconds = 3,
        JitterMinFactor = 0.25f,
        JitterMaxFactor = 0.5f,
        IdleCheckEnabled = true,
        IdlePopulationThreshold = 40,
        PlanetCooldownSeconds = 1,
        DefenseEnabled = true,
        AttackEnabled = true,
        GatherEnabled = false,
        Defense = new AiDefenseParameters
        {
            RequiresEnemy = false,
            DistanceJitter = 0,
            ConsiderIncomingEnemies = false,
            AllowEqual = false,
            EnemyCoefficient = 2,
            AllyCoefficient = 1,
            DamageEstimateCoefficient = 0,
        },
        Attack = new AiAttackParameters
        {
            ExclusionThresholdMultiplier = 1.0f,
            ExclusionScopeNeutralOnly = true,
            ExcludeWeakEnemy = false,
            DistanceJitter = 32,
            ExcludeCapturingSenders = false,
            ConsiderIncomingEnemies = false,
            AllowEqual = false,
            EnemyCoefficient = 2,
            AllyCoefficient = 0.5f,
            LowerBoundMultiplier = 2,
            AllOutPriority = AiAllOutPriority.AllOutFirst,
            DamageEstimateCoefficient = 0,
        },
        Gather = new AiGatherParameters
        {
            OwnTeamSendersOnly = false,
            SortAddsStrongestEnemy = false,
            ConsiderIncomingEnemies = false,
            WarpValueBonus = 0,
            DamageEstimateCoefficient = 0,
        },
    };

    /// 预设：防御/进攻/聚兵全部启用，节奏适中
    public static readonly Ai Smart = new()
    {
        ActionIntervalSeconds = 1.5f,
        InitialDelaySeconds = 1.5f,
        JitterMinFactor = 0.25f,
        JitterMaxFactor = 0.5f,
        IdleCheckEnabled = true,
        IdlePopulationThreshold = 40,
        PlanetCooldownSeconds = 1,
        DefenseEnabled = true,
        AttackEnabled = true,
        GatherEnabled = true,
        Defense = new AiDefenseParameters
        {
            RequiresEnemy = true,
            DistanceJitter = 32,
            ConsiderIncomingEnemies = true,
            AllowEqual = true,
            EnemyCoefficient = 2,
            AllyCoefficient = 1,
            DamageEstimateCoefficient = 1f / 4.5f,
        },
        Attack = new AiAttackParameters
        {
            ExclusionThresholdMultiplier = 1.5f,
            ExclusionScopeNeutralOnly = false,
            ExcludeWeakEnemy = false,
            DistanceJitter = 32,
            ExcludeCapturingSenders = true,
            ConsiderIncomingEnemies = true,
            AllowEqual = false,
            EnemyCoefficient = 2,
            AllyCoefficient = 0.5f,
            LowerBoundMultiplier = 2,
            AllOutPriority = AiAllOutPriority.LowerBoundFirst,
            DamageEstimateCoefficient = 1f / 4.5f,
        },
        Gather = new AiGatherParameters
        {
            OwnTeamSendersOnly = false,
            SortAddsStrongestEnemy = true,
            ConsiderIncomingEnemies = true,
            WarpValueBonus = 0,
            DamageEstimateCoefficient = 1f / 4.5f,
        },
    };

    /// 预设：仅进攻与聚兵，节奏最快
    public static readonly Ai Dark = new()
    {
        ActionIntervalSeconds = 0.25f,
        InitialDelaySeconds = 0.25f,
        JitterMinFactor = 0.25f,
        JitterMaxFactor = 0.5f,
        IdleCheckEnabled = true,
        IdlePopulationThreshold = 40,
        PlanetCooldownSeconds = 1,
        DefenseEnabled = false,
        AttackEnabled = true,
        GatherEnabled = true,
        Defense = new AiDefenseParameters
        {
            RequiresEnemy = false,
            DistanceJitter = 0,
            ConsiderIncomingEnemies = false,
            AllowEqual = false,
            EnemyCoefficient = 0,
            AllyCoefficient = 0,
            DamageEstimateCoefficient = 0,
        },
        Attack = new AiAttackParameters
        {
            ExclusionThresholdMultiplier = 2.0f,
            ExclusionScopeNeutralOnly = false,
            ExcludeWeakEnemy = true,
            DistanceJitter = 32,
            ExcludeCapturingSenders = true,
            ConsiderIncomingEnemies = true,
            AllowEqual = true,
            EnemyCoefficient = 2,
            AllyCoefficient = 0.5f,
            LowerBoundMultiplier = 2,
            AllOutPriority = AiAllOutPriority.LowerBoundFirst,
            DamageEstimateCoefficient = 1f / 4.5f,
        },
        Gather = new AiGatherParameters
        {
            OwnTeamSendersOnly = true,
            SortAddsStrongestEnemy = false,
            ConsiderIncomingEnemies = false,
            WarpValueBonus = 0,
            DamageEstimateCoefficient = 1f / 4.5f,
        },
    };
}

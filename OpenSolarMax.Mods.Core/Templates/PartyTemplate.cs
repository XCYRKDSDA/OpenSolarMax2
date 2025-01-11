using Arch.Core;
using Arch.Core.Extensions;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Templates;

/// <summary>
/// 阵营模板。
/// 将实体配置为一个白色的无法生产也无法进攻也无法死亡的阵营实体
/// </summary>
public class PartyTemplate : ITemplate
{
    #region Options

    /// <summary>
    /// 阵营的代表色
    /// </summary>
    public required Color Color { get; set; }

    /// <summary>
    /// 生产一个该阵营单位需要的工作量
    /// </summary>
    public required float Workload { get; set; }

    /// <summary>
    /// 每个该阵营的单位每秒可以造成的伤害
    /// </summary>
    public required float Attack { get; set; }

    /// <summary>
    /// 每个该阵营的单位最多可以承受的伤害
    /// </summary>
    public required float Health { get; set; }

    #endregion

    private static readonly Archetype _archetype = new(
        // 依赖关系
        typeof(Dependence.AsDependent),
        typeof(Dependence.AsDependency),
        // 阵营参考值
        typeof(PartyReferenceColor),
        // 阵营属性
        typeof(Producible),
        typeof(Combatable),
        typeof(Shippable),
        typeof(ColonizationAbility),
        // 隶属关系
        typeof(InParty.AsParty),
        typeof(PartyPopulationRegistry)
    );

    public Archetype Archetype => _archetype;

    public void Apply(Entity entity)
    {
        ref var refColor = ref entity.Get<PartyReferenceColor>();
        refColor.Value = Color;

        ref var producible = ref entity.Get<Producible>();
        producible.WorkloadPerShip = Workload;

        ref var combatable = ref entity.Get<Combatable>();
        combatable.AttackPerUnitPerSecond = Attack;
        combatable.MaximumDamagePerUnit = Health;

        ref var shippable = ref entity.Get<Shippable>();
        shippable.Speed = 100;

        ref var colonizationAbility = ref entity.Get<ColonizationAbility>();
        colonizationAbility.ProgressPerSecond = 1;
    }
}

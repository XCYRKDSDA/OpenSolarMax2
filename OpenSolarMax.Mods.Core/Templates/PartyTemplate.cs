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
    public Archetype Archetype => Archetypes.Party;

    public void Apply(Entity entity)
    {
        ref var refColor = ref entity.Get<PartyReferenceColor>();
        ref var producible = ref entity.Get<Producible>();
        ref var combatable = ref entity.Get<Combatable>();
        ref var shippable = ref entity.Get<Shippable>();
        ref var colonizationAbility = ref entity.Get<ColonizationAbility>();

        refColor.Value = Color.White;

        producible.WorkloadPerShip = float.PositiveInfinity;

        combatable.AttackPerUnitPerSecond = 0;
        combatable.MaximumDamagePerUnit = float.PositiveInfinity;

        shippable.Speed = 100;

        colonizationAbility.ProgressPerSecond = 1;
    }
}

using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;
using OpenSolarMax.Mods.Core.Utils;
using Archetype = OpenSolarMax.Game.Utils.Archetype;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 结算生产系统. 在所有推进了生产的星球上计算是否产生新单位
/// </summary>
[StructuralChangeSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(ProgressProductionSystem))]
public sealed partial class SettleProductionSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly UnitBornPulseTemplate _unitBornPulseTemplate = new(assets);

    [Query]
    [All<ProductionAbility, ProductionState, InParty.AsAffiliate>]
    private void SettleProduction(Entity planet, in ProductionAbility ability, ref ProductionState state,
                                  in InParty.AsAffiliate partyRelationship)
    {
        if (partyRelationship.Relationship is null)
            return;
        var party = partyRelationship.Relationship!.Value.Copy.Party;

        ref readonly var producible = ref party.Entity.Get<Producible>();

        // 生产一个新部队
        if (state.Progress >= producible.WorkloadPerShip)
        {
            var newShip = World.Make(new ShipTemplate(assets) { Party = party, Planet = planet.Reference() });

            // 添加出生后动画
            newShip.Add(new UnitPostBornEffect() { TimeElapsed = TimeSpan.Zero });

            // 生成出生动画
            var pulse = World.Construct(_unitBornPulseTemplate.Archetype);
            _unitBornPulseTemplate.Apply(pulse);
            pulse.Get<Sprite>().Color = party.Entity.Get<PartyReferenceColor>().Value;
            World.Create(new TreeRelationship<RelativeTransform>(newShip.Reference(), pulse.Reference()),
                         new RelativeTransform());
            World.Create(new Dependence(pulse.Reference(), newShip.Reference()));

            // 减去对应工作量
            state.Progress -= producible.WorkloadPerShip;
        }
    }
}

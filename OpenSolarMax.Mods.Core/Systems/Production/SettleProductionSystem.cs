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

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 结算生产系统. 在所有推进了生产的星球上计算是否产生新单位
/// </summary>
[StructuralChangeSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
[ExecuteAfter(typeof(ProgressProductionSystem))]
[ExecuteBefore(typeof(SettleColonizationSystem))]
public sealed partial class SettleProductionSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ProductionState, InParty.AsAffiliate>]
    private void SettleProduction(Entity planet, in ProductionState state, in InParty.AsAffiliate partyRelationship)
    {
        if (partyRelationship.Relationship is null)
            return;
        var party = partyRelationship.Relationship!.Value.Copy.Party;

        // 生产一个新部队
        for (int i = 0; i < state.UnitsProducedThisFrame; i++)
        {
            var newShip = World.Make(new ShipTemplate(assets) { Party = party, Planet = planet });

            // 添加出生后动画
            newShip.Add(new UnitPostBornEffect() { TimeElapsed = TimeSpan.Zero });

            // 生成出生动画
            _ = World.Make(new UnitBornPulseTemplate(assets)
            {
                Unit = newShip,
                Color = party.Get<PartyReferenceColor>().Value
            });
        }
    }
}

using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Assets;
using OpenSolarMax.Game.Modding.Concept;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Concepts;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 结算生产系统. 在所有推进了生产的星球上计算是否产生新单位
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[ReadCurr(typeof(ProductionState)), ReadPrev(typeof(InParty.AsAffiliate)), ReadPrev(typeof(PartyReferenceColor))]
[ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class SettleProductionSystem(World world, IAssetsManager assets, IConceptFactory factory)
    : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<ProductionState, InParty.AsAffiliate>]
    private void SettleProduction(Entity planet, in ProductionState state, in InParty.AsAffiliate partyRelationship,
                                  [Data] CommandBuffer commandBuffer)
    {
        if (partyRelationship.Relationship is null)
            return;
        var party = partyRelationship.Relationship!.Value.Copy.Party;

        // 生产一个新部队
        for (int i = 0; i < state.UnitsProducedThisFrame; i++)
        {
            var newShip = factory.Make(world, commandBuffer, ConceptNames.Ship,
                                       new ShipDescription() { Party = party, Planet = planet });

            // 添加出生后动画
            commandBuffer.Add(newShip, new UnitPostBornEffect() { TimeElapsed = TimeSpan.Zero });

            // 生成出生动画
            _ = world.Make(commandBuffer, new UnitBornPulseTemplate(assets)
            {
                Unit = newShip,
                Color = party.Get<PartyReferenceColor>().Value
            });
        }
    }

    public void Update(CommandBuffer commandBuffer) => SettleProductionQuery(world, commandBuffer);
}

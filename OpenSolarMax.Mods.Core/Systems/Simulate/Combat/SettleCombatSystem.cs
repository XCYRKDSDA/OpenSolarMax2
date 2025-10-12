using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Nine.Assets;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 战斗结算系统。根据星球上各阵营的战斗值进行战斗减员
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[ReadPrev(typeof(AnchoredShipsRegistry)), ReadPrev(typeof(Combatable)), ReadPrev(typeof(Sprite)),
 ReadPrev(typeof(AbsoluteTransform))]
[Iterate(typeof(Battlefield)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
// 先量变再质变
[ExecuteAfter(typeof(ProgressCombatSystem))]
public sealed partial class SettleCombatSystem(World world, IAssetsManager assets) : ICalcSystemWithStructuralChanges
{
    [Query]
    [All<AnchoredShipsRegistry, Battlefield>]
    private void SettleCombat(in AnchoredShipsRegistry shipsRegistry, ref Battlefield battle,
                              [Data] CommandBuffer commandBuffer)
    {
        // 考察各个阵营的破坏度
        foreach (var party in battle.FrontlineDamage.Keys)
        {
            ref readonly var partyCombatAbility = ref party.Get<Combatable>();
            using var shipEnumerator = shipsRegistry.Ships[party].GetEnumerator();

            // 根据前线战损逐个移除单位
            var damage = battle.FrontlineDamage[party];
            while (damage >= partyCombatAbility.MaximumDamagePerUnit && shipEnumerator.MoveNext())
            {
                damage -= partyCombatAbility.MaximumDamagePerUnit;

                var ship = shipEnumerator.Current;

                var color = ship.Get<Sprite>().Color;
                var position = ship.Get<AbsoluteTransform>().Translation;

                // 生成闪光
                _ = world.Make(commandBuffer, new UnitFlareTemplate(assets) { Color = color, Position = position });

                // 生成冲击波
                _ = world.Make(commandBuffer, new UnitPulseTemplate(assets) { Color = color, Position = position });

                // 移除单位
                commandBuffer.Destroy(ship);
            }
            battle.FrontlineDamage[party] = damage;
        }
    }

    public void Update(CommandBuffer commandBuffer) => SettleCombatQuery(world, commandBuffer);
}

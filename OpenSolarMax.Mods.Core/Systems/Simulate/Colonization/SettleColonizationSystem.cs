using Arch.Buffer;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Utils;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Templates;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 监测殖民进度，切换或者移除殖民状态，同时播放动画
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[ReadPrev(typeof(AbsoluteTransform)), ReadPrev(typeof(ReferenceSize)), ReadPrev(typeof(PartyReferenceColor))]
[ReadPrev(typeof(InParty.AsAffiliate), withEntities: true), Iterate(typeof(ColonizationState)), ChangeStructure]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
// 先计算进度，再判断是否完成殖民
[ExecuteAfter(typeof(ProgressColonizationSystem))]
public sealed partial class SettleColonizationSystem(World world, IAssetsManager assets)
    : ICalcSystemWithStructuralChanges
{
    private void CreateHaloExplosion(CommandBuffer commandBuffer, Entity planet, Color color)
    {
        ref var planetAbsoluteTransform = ref planet.Get<AbsoluteTransform>();
        ref readonly var refSize = ref planet.Get<ReferenceSize>();
        _ = world.Make(commandBuffer, new HaloExplosionTemplate(assets)
        {
            Color = color,
            Position = planetAbsoluteTransform.Translation,
            PlanetRadius = refSize.Radius
        });
    }

    [Query]
    [All<ColonizationState, InParty.AsAffiliate>]
    private void SettleColonization(Entity planet, ref ColonizationState state, in InParty.AsAffiliate asPartyAffiliate,
                                    [Data] CommandBuffer commandBuffer)
    {
        var planetParty = asPartyAffiliate.Relationship?.Copy.Party;

        if (state.Event == ColonizationEvent.Finished)
        {
            // 不管怎样，先开香槟
            CreateHaloExplosion(commandBuffer, planet, state.Party.Get<PartyReferenceColor>().Value);

            // 完成殖民
            if (planetParty is null)
                world.Make(commandBuffer, new InPartyTemplate() { Party = state.Party, Affiliate = planet });
        }
        else if (state.Event == ColonizationEvent.Destroyed)
        {
            // 开香槟
            CreateHaloExplosion(commandBuffer, planet, Color.White);

            // 解除当前阵营的殖民
            if (planetParty is not null)
                commandBuffer.Destroy(asPartyAffiliate.Relationship!.Value.Ref);
        }
    }

    public void Update(CommandBuffer commandBuffer) => SettleColonizationQuery(world, commandBuffer);
}

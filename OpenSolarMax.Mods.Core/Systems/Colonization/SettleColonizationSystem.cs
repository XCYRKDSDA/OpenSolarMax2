using Arch.Buffer;
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
/// 监测殖民进度，切换或者移除殖民状态，同时播放动画
/// </summary>
[StructuralChangeSystem]
[ExecuteBefore(typeof(IndexPartyAffiliationSystem))]
public sealed partial class SettleColonizationSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    private void CreateHaloExplosion(Entity planet, Color color)
    {
        ref var planetAbsoluteTransform = ref planet.Get<AbsoluteTransform>();
        ref readonly var refSize = ref planet.Get<ReferenceSize>();
        _ = World.Make(new HaloExplosionTemplate(assets)
        {
            Color = color,
            Position = planetAbsoluteTransform.Translation,
            PlanetRadius = refSize.Radius
        });
    }

    [Query]
    [All<Colonizable, ColonizationState, ColonizationStateHistory, InParty.AsAffiliate>]
    private void SettleColonization(Entity planet, in Colonizable colonizable,
                                    ref ColonizationState state, ref ColonizationStateHistory history,
                                    in InParty.AsAffiliate asPartyAffiliate)
    {
        var planetParty = asPartyAffiliate.Relationship?.Copy.Party;

        if (history.Previous.Progress < colonizable.Volume && state.Progress >= colonizable.Volume)
        {
            // 不管怎样，先开香槟
            CreateHaloExplosion(planet, state.Party.Entity.Get<PartyReferenceColor>().Value);

            // 完成殖民
            if (planetParty is null)
                World.Make(new InPartyTemplate() { Party = state.Party, Affiliate = planet.Reference() });
        }
        else if (state.Party != history.Previous.Party)
        {
            if (history.Previous.Party != EntityReference.Null)
                // 如果之前有阵营在殖民，则开香槟
                CreateHaloExplosion(planet, Color.White);

            // 解除当前阵营的殖民
            if (planetParty is not null)
                World.Destroy(asPartyAffiliate.Relationship!.Value.Ref);
        }
    }

    public override void Update(in GameTime t)
    {
        SettleColonizationQuery(World);
        _commandBuffer.Playback(World);
    }

    public override void Dispose()
    {
        base.Dispose();
        _commandBuffer.Dispose();
    }
}

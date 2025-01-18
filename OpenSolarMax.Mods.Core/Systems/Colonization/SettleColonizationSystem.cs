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
    [All<Colonizable, ColonizationState, InParty.AsAffiliate, AnchoredShipsRegistry>]
    private void SettleColonization(Entity planet,
                                    in Colonizable colonizable, ref ColonizationState state,
                                    in InParty.AsAffiliate asPartyAffiliate,
                                    in AnchoredShipsRegistry shipsRegistry)
    {
        if (shipsRegistry.Ships.Count != 1)
            return;

        var colonizeParty = shipsRegistry.Ships.First().Key;
        var planetParty = asPartyAffiliate.Relationship?.Copy.Party;

        if (state.Progress > colonizable.Volume)
        {
            state.Progress = colonizable.Volume;

            // 完成殖民
            if (planetParty is null)
                World.Create(new InParty(state.Party, planet.Reference()));

            CreateHaloExplosion(planet, state.Party.Entity.Get<PartyReferenceColor>().Value);

            // 移除“占领中”组件
            _commandBuffer.Remove<ColonizationState>(planet);
        }
        else if (state.Progress < 0)
        {
            state.Progress = -state.Progress;
            state.Party = colonizeParty;

            // 解除当前阵营的殖民
            if (planetParty is not null)
                World.Destroy(asPartyAffiliate.Relationship!.Value.Ref);

            CreateHaloExplosion(planet, Color.White);
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

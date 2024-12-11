using System.Diagnostics;
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
using FmodEventDescription = FMOD.Studio.EventDescription;

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

    private readonly HaloExplosionTemplate _haloExplosionTemplate = new(assets);

    private FmodEventDescription _colonizedSoundEvent =
        assets.Load<FmodEventDescription>("Sounds/Master.bank:/PlanetColonized");

    private void CreateHaloExplosion(Entity planet, Color color)
    {
        var halo = World.Construct(_haloExplosionTemplate.Archetype);
        _haloExplosionTemplate.Apply(halo);

        // 摆放位置
        Debug.Assert(planet.Has<AbsoluteTransform>());
        ref var planetAbsoluteTransform = ref planet.Get<AbsoluteTransform>();
        halo.Get<AbsoluteTransform>() = planetAbsoluteTransform;
        halo.Get<AbsoluteTransform>().Translation.Z = 1000; // 一定位于最前边

        // 设置颜色
        halo.Get<Sprite>().Color = color;

        // 设置尺寸
        ref readonly var refSize = ref planet.Get<ReferenceSize>();
        halo.Get<Animation>().RawClip!.Parameters["SCALE"] = refSize.Radius / 60;

        // 播放音效
        _colonizedSoundEvent.createInstance(out var instance);
        World.Create(new SoundEffect() { EventInstance = instance }, planetAbsoluteTransform);
        instance.start();
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

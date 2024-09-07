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
using Fmod3DAttributes = FMOD.ATTRIBUTES_3D;

namespace OpenSolarMax.Mods.Core.Systems;

[CoreUpdateSystem]
public sealed partial class UpdateColonizationSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ColonizationState, TreeRelationship<Party>.AsChild, AnchoredShipsRegistry>]
    private static void UpdateColonization([Data] GameTime time,
                                           ref ColonizationState state, in TreeRelationship<Party>.AsChild asPartyChild,
                                           in AnchoredShipsRegistry shipsRegistry)
    {
        if (shipsRegistry.Ships.Count != 1)
            return;

        var colonizeParty = shipsRegistry.Ships.First().Key;
        var shipsNum = shipsRegistry.Ships.First().Count();

        var deltaProgress = shipsNum * colonizeParty.Entity.Get<ColonizationAbility>().ProgressPerSecond
                                     * (float)time.ElapsedGameTime.TotalSeconds;

        if (state.Party == colonizeParty || colonizeParty == EntityReference.Null)
            state.Progress += deltaProgress;
        else
            state.Progress -= deltaProgress;
    }
}

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
        var halo = world.Construct(_haloExplosionTemplate.Archetype);
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
    [All<Colonizable, ColonizationState, TreeRelationship<Party>.AsChild, AnchoredShipsRegistry>]
    private void SettleColonization(Entity planet,
                                    in Colonizable colonizable, ref ColonizationState state,
                                    in TreeRelationship<Party>.AsChild asPartyChild,
                                    in AnchoredShipsRegistry shipsRegistry)
    {
        if (shipsRegistry.Ships.Count != 1)
            return;

        var colonizeParty = shipsRegistry.Ships.First().Key;
        var planetParty = asPartyChild.Index.Parent;

        if (state.Progress > colonizable.Volume)
        {
            state.Progress = colonizable.Volume;

            // 完成殖民
            if (planetParty == EntityReference.Null)
                World.Create(new TreeRelationship<Party>(state.Party, planet.Reference()));

            CreateHaloExplosion(planet, state.Party.Entity.Get<PartyReferenceColor>().Value);

            // 移除“占领中”组件
            _commandBuffer.Remove<ColonizationState>(planet);
        }
        else if (state.Progress < 0)
        {
            state.Progress = -state.Progress;
            state.Party = colonizeParty;

            // 解除当前阵营的殖民 
            if (planetParty != EntityReference.Null)
                World.Destroy(asPartyChild.Index.Relationship);

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

[StructuralChangeSystem]
[ExecuteBefore(typeof(IndexPartyAffiliationSystem))]
[ExecuteBefore(typeof(SettleColonizationSystem))]
public sealed partial class StartColonizationSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<Colonizable, TreeRelationship<Party>.AsChild, AnchoredShipsRegistry>]
    [None<ColonizationState>]
    private void StartColonization(Entity planet, in Colonizable colonizable,
                                   in TreeRelationship<Party>.AsChild asPartyChild,
                                   in AnchoredShipsRegistry shipsRegistry)
    {
        if (shipsRegistry.Ships.Count != 1)
            return;
        var shipParty = shipsRegistry.Ships.First().Key;

        if (asPartyChild.Index.Parent == shipParty)
            return;

        // 如果当前星球没有所属阵营，则停靠单位阵营直接开始进行自己的殖民；
        // 如果当前星球已有阵营，则停靠单位阵营需要先破坏现有阵营的殖民度
        if (asPartyChild.Index.Parent == EntityReference.Null)
            _commandBuffer.Add(planet, new ColonizationState() { Party = shipParty, Progress = 0 });
        else
        {
            _commandBuffer.Add(planet, new ColonizationState()
            {
                Party = asPartyChild.Index.Parent,
                Progress = colonizable.Volume
            });
        }
    }

    public override void Update(in GameTime t)
    {
        StartColonizationQuery(World);
        _commandBuffer.Playback(World);
    }

    public override void Dispose()
    {
        base.Dispose();
        _commandBuffer.Dispose();
    }
}

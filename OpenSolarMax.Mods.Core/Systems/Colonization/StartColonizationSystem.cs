using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 对有单一阵营单位的可殖民的实体，自动开始殖民、添加殖民状态
/// </summary>
[StructuralChangeSystem]
[ExecuteBefore(typeof(IndexPartyAffiliationSystem))]
[ExecuteBefore(typeof(SettleColonizationSystem))]
#pragma warning disable CS9113 // 参数未读。
public sealed partial class StartColonizationSystem(World world, IAssetsManager assets)
#pragma warning restore CS9113 // 参数未读。
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<Colonizable, InParty.AsAffiliate, AnchoredShipsRegistry>]
    [None<ColonizationState>]
    private void StartColonization(Entity planet, in Colonizable colonizable,
                                   in InParty.AsAffiliate asPartyAffiliate,
                                   in AnchoredShipsRegistry shipsRegistry)
    {
        if (shipsRegistry.Ships.Count != 1)
            return;
        var shipParty = shipsRegistry.Ships.First().Key;

        if (asPartyAffiliate.Relationship?.Copy.Party == shipParty)
            return;

        // 如果当前星球没有所属阵营，则停靠单位阵营直接开始进行自己的殖民；
        // 如果当前星球已有阵营，则停靠单位阵营需要先破坏现有阵营的殖民度
        if (asPartyAffiliate.Relationship is null)
            _commandBuffer.Add(planet, new ColonizationState() { Party = shipParty, Progress = 0 });
        else
        {
            _commandBuffer.Add(planet, new ColonizationState()
            {
                Party = asPartyAffiliate.Relationship!.Value.Copy.Party,
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

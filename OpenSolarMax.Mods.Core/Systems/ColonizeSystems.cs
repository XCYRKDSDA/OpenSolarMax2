using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

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
        }
        else if (state.Progress < 0)
        {
            state.Progress = -state.Progress;
            state.Party = colonizeParty;

            // 解除当前阵营的殖民 
            if (planetParty != EntityReference.Null)
                World.Destroy(asPartyChild.Index.Relationship);
        }
    }
}

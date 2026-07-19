// 整文件禁用：ECS 框架层重构后待迁移
#if false
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(TreeRelationship<Anchorage>.AsParent)), Write(typeof(AnchoredShipsRegistry))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class UpdateShipRegistrySystem(World world) : ICalcSystem
{
    [Query]
    [All<TreeRelationship<Anchorage>.AsParent, AnchoredShipsRegistry>]
    private static void CountAnchoredShips(
        in TreeRelationship<Anchorage>.AsParent asAnchorageParent,
        ref AnchoredShipsRegistry shipRegistry
    )
    {
        shipRegistry.Ships =
            (Lookup<Entity, Entity>)
                asAnchorageParent.Relationships.Values.ToLookup(
                    copy => copy.Child.Get<InTeam.AsAffiliate>().Relationship!.Value.Copy.Team,
                    copy => copy.Child
                );
    }

    public void Update() => CountAnchoredShipsQuery(world);
}

#endif

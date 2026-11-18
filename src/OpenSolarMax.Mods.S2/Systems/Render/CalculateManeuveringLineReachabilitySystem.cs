using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 查询拖拽线段的出发天体到目标的可达性
/// </summary>
[LateUpdate]
[RenderSystem]
[ReadCurr(typeof(PlanetManeuveringLineSource.AsLine))]
[ReadCurr(typeof(ManeuveringLineTarget.AsLine))]
[ReadCurr(typeof(ReachabilityRelation.AsDeparture))]
[ReadCurr(typeof(InTeam.AsAffiliate))]
[ReadCurr(typeof(Reachability))]
[Calc(typeof(ManeuveringLineReachability))]
public sealed partial class CalculateManeuveringLineReachabilitySystem(World world) : ICalcSystem
{
    [Query]
    [All<
        PlanetManeuveringLineSource.AsLine,
        ManeuveringLineTarget.AsLine,
        ManeuveringLineReachability
    >]
    private static void UpdateReachability(
        Entity entity,
        ref ManeuveringLineReachability reachability
    )
    {
        // 两个组件类型都叫 AsLine，Arch.System.SourceGenerator 生成时会冲突，因此只能专门 Get
        var asLineInSource = entity
            .Get<PlanetManeuveringLineSource.AsLine>()
            .Relationship!.Value.Copy;
        var asLineInTarget = entity.Get<ManeuveringLineTarget.AsLine>().Relationship!.Value.Copy;

        var departure = asLineInSource.Planet;
        var destination = asLineInTarget.Target;

        var result = departure
            .Get<ReachabilityRelation.AsDeparture>()
            .Relationships.First(kv => kv.Value.Destination == destination)
            .Key.Get<Reachability>();

        // 按视图所属阵营是否占领出发天体，取对应那一侧的判定结果
        var team = asLineInSource.View.Get<InTeam.AsAffiliate>().Relationship!.Value.Copy.Team;
        var owner = departure.Get<InTeam.AsAffiliate>().Relationship?.Copy.Team;
        reachability.Reachable = team == owner ? result.OwnerReachable : result.NonOwnerReachable;
    }

    public void Update() => UpdateReachabilityQuery(world);
}

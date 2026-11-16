using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
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
        var departure = entity
            .Get<PlanetManeuveringLineSource.AsLine>()
            .Relationship!.Value.Copy.Planet;
        var destination = entity
            .Get<ManeuveringLineTarget.AsLine>()
            .Relationship!.Value.Copy.Target;

        reachability.Reachable = departure
            .Get<ReachabilityRelation.AsDeparture>()
            .Relationships.First(kv => kv.Value.Destination == destination)
            .Key.Get<Reachability>()
            .Reachable;
    }

    public void Update() => UpdateReachabilityQuery(world);
}

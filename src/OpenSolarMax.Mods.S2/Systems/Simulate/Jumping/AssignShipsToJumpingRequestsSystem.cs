using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Systems;
using OpenSolarMax.Mods.S2.Components;

namespace OpenSolarMax.Mods.S2.Systems;

/// <summary>
/// 为尚未分配舰船的<see cref="StartJumpingRequest"/>分配舰船，写入<see cref="StartJumpingAssignedShips"/>。
/// 跳跃与跃迁两类起飞系统都只从该列表取船，故分配只在此处发生一次。
/// <para>
/// 分配以出发天体为入口：逐个取本天体锚定的舰船与该天体名下的全部请求，顺着请求逐艘切分。
/// </para>
/// </summary>
[SimulateSystem, LateUpdate]
[
    ReadCurr(typeof(AnchoredShipsRegistry)),
    ReadCurr(typeof(StartJumpingRequest.AsDeparture)),
    Calc(typeof(StartJumpingAssignedShips))
]
[ExecuteAfter(
    typeof(ApplyAnimationSystem),
    "默认动画系统优先执行",
    typeof(StartJumpingAssignedShips)
)]
public sealed partial class AssignShipsToJumpingRequestsSystem(World world) : ICalcSystem
{
    [Query]
    [All<AnchoredShipsRegistry, StartJumpingRequest.AsDeparture>]
    private static void AssignShips(
        in AnchoredShipsRegistry registry,
        in StartJumpingRequest.AsDeparture requests
    )
    {
        // 逐阵营分配
        foreach (var group in requests.Relationships.GroupBy(r => r.Value.Team))
        {
            using var ships = registry.Ships[group.Key].GetEnumerator();
            foreach (var (requestEntity, request) in group)
            {
                ref var assigned = ref requestEntity.Get<StartJumpingAssignedShips>();
                // 分配后的飞船应由起飞系统在本轮内消费掉；仍留在请求中说明状态已经出错
                Debug.Assert(assigned.Ships is null, $"出发请求 {requestEntity} 已分配飞船");

                assigned.Ships = [];
                while (assigned.Ships.Count < request.ExpectedNum && ships.MoveNext())
                    assigned.Ships.Add(ships.Current);
            }
        }
    }

    public void Update() => AssignShipsQuery(world);
}

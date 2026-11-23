using Arch.Core;
using OpenSolarMax.Mods.Common.SourceGenerators;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 开始运输请求。描述一个开始运输的请求
/// </summary>
[Relationship]
public readonly partial struct StartJumpingRequest(
    Entity departure,
    Entity destination,
    Entity team,
    int expectedNum
)
{
    /// <summary>
    /// 出发天体实体。一个天体可以同时作为多个请求的起点
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Departure = departure;

    /// <summary>
    /// 目的天体实体。一个天体可以同时作为多个请求的目标
    /// </summary>
    [Participant(exclusive: false)]
    public readonly Entity Destination = destination;

    /// <summary>
    /// 请求所属的阵营
    /// </summary>
    public readonly Entity Team = team;

    /// <summary>
    /// 期望的舰船数量
    /// </summary>
    public readonly int ExpectedNum = expectedNum;
}

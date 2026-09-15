using Arch.Core;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 开始运输请求。描述一个开始运输的请求
/// </summary>
public struct StartJumpingRequest
{
    public Entity Departure;

    public Entity Destination;

    public Entity Team;

    public int ExpectedNum;
}

using Arch.Core;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 开始运输请求。描述一个开始运输的请求
/// </summary>
public struct StartShippingRequest
{
    public EntityReference Departure;

    public EntityReference Destination;

    public EntityReference Party;

    public int ExpectedNum;
}

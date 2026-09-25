using Arch.Core;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 飞行请求已分配得到的舰船。为 null 表示尚未分配；非 null 表示已分配（列表可能为空）。
/// </summary>
public struct StartJumpingAssignedShips
{
    public List<Entity>? Ships;
}

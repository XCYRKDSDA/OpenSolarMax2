using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 转化 dilator 的状态：是否已经触发过转化波
/// </summary>
[Component]
public struct ConvertingDilatorState
{
    /// <summary>
    /// 是否已经触发过转化波
    /// </summary>
    public bool Triggered;
}

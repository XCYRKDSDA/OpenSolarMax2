using OpenSolarMax.Ecs;

namespace OpenSolarMax.Mods.S2.Components;

[Component]
public struct ConvertingDilatorState
{
    /// <summary>
    /// 是否已经触发过转化波
    /// </summary>
    public bool Triggered;
}

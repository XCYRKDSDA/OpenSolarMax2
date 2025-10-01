using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 公转状态组件。描述当前实体绕其当前轨道公转的实时状态。
/// 属于“状态”类型组件
/// </summary>
[Component]
public struct RevolutionState
{
    /// <summary>
    /// 当前相位
    /// </summary>
    public float Phase;
}

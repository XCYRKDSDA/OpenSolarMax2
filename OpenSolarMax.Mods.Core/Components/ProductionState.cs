using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 生产状态组件。描述星球当前的生产进度
/// </summary>
[Component]
public struct ProductionState()
{
    /// <summary>
    /// 这一帧当前星球是否能够生产
    /// </summary>
    public bool CanProduce = false;

    /// <summary>
    /// 当前单位的生产进度
    /// </summary>
    public float Progress = 0;

    /// <summary>
    /// 这一帧完成生产的单位个数
    /// </summary>
    public int UnitsProducedThisFrame = 0;
}

using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 选择圈的视觉效果状态。由动画系统驱动 Alpha 和 Scale，渲染系统从 configs 读取颜色和粗细。
/// </summary>
[Component]
public struct SelectionRingVisual()
{
    /// <summary>
    /// 透明度，由动画驱动。初始为 1（完全可见），淡出动画后变为 0（隐藏）。
    /// </summary>
    public float Alpha = 1f;

    /// <summary>
    /// 缩放因子，由动画驱动。初始为 1（正常大小），淡出动画时可增大或减小。
    /// </summary>
    public float Scale = 1f;
}

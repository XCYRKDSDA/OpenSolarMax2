using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 阵营的视觉呈现选项
/// </summary>
[Component]
public struct TeamVisualization()
{
    /// <summary>
    /// 是否隐藏天体上该阵营的舰船数目文字
    /// </summary>
    public bool HideShipCount;
}

using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 阵营推荐的视觉样式。用于设置所有属于该阵营的实体的颜色与混合模式
/// </summary>
[Component]
public struct RecommendedVisualStyle
{
    /// <summary>
    /// 阵营的代表色
    /// </summary>
    public Color Color;

    /// <summary>
    /// 发光外观（<see cref="VisualStyle.Effect"/>）的混合模式。实心外观恒用 Alpha
    /// </summary>
    public SpriteBlend Blend;
}

using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.S2.Components;

/// <summary>
/// 阵营参考颜色。用于设置所有属于该阵营的实体的颜色
/// </summary>
[Component]
public struct TeamReferenceColor
{
    public Color Value;
}

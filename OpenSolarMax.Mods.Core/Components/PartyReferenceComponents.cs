using Microsoft.Xna.Framework;
using OpenSolarMax.Game.System;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 阵营参考颜色。用于设置所有属于该阵营的实体的颜色
/// </summary>
[Component]
public struct PartyReferenceColor
{
    public Color Value;
}

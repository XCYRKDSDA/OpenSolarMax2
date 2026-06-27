using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Core.Components;

[Component]
public struct Projection
{
    /// <summary>
    /// 世界坐标到 NDC 的变换矩阵
    /// </summary>
    public Matrix WorldToNdc;

    /// <summary>
    /// 世界坐标到屏幕坐标的变换矩阵
    /// </summary>
    public Matrix WorldToScreen;

    /// <summary>
    /// 屏幕坐标到 NDC 的变换矩阵
    /// </summary>
    public Matrix ScreenToNdc;
}

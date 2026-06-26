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
    /// 世界坐标到画布坐标的变换矩阵
    /// </summary>
    public Matrix WorldToCanvas;

    /// <summary>
    /// 画布坐标到 NDC 的变换矩阵
    /// </summary>
    public Matrix CanvasToNdc;
}

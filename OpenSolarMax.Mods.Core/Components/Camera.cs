using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Mods.Core.Components;

/// <summary>
/// 相机组件。描述成像参数和输出位置
/// </summary>
public struct Camera
{
    public float Width, Height;

    public float ZNear, ZFar;

    /// <summary>
    /// 根据该相机绘制的图像将输出到窗口的视口
    /// </summary>
    public Viewport Output;
}

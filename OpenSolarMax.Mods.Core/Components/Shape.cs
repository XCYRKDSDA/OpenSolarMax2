using Microsoft.Xna.Framework;
using Nine.Graphics;

namespace OpenSolarMax.Mods.Core.Components;

public struct Shape()
{
    /// <summary>
    /// 精灵纹理
    /// </summary>
    public TextureRegion? Texture = null;

    /// <summary>
    /// 精灵的掩膜颜色
    /// </summary>
    public Color Color = Color.White;

    /// <summary>
    /// 纹理逻辑边框在世界中的尺寸
    /// </summary>
    public Vector2 Size = Vector2.Zero;

    /// <summary>
    /// 精灵逻辑原点相对实体的坐标
    /// </summary>
    public Vector2 Position = Vector2.Zero;

    /// <summary>f
    /// 精灵相对实体的旋转
    /// </summary>
    public float Rotation = 0;

    /// <summary>
    /// 精灵的缩放
    /// </summary>
    public Vector2 Scale = Vector2.One;
}

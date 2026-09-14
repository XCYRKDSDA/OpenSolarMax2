using Microsoft.Xna.Framework;
using Nine.Graphics;
using OpenSolarMax.Game.Modding.ECS;

namespace OpenSolarMax.Mods.Core.Components;

public enum SpriteBlend
{
    Alpha,
    Additive,
    Opaque,
    NonPremultiplied,
}

public struct TextureUV<T>
{
    public T LeftTop;
    public T RightTop;
    public T LeftBottom;
    public T RightBottom;

    public static implicit operator TextureUV<T>(T color) =>
        new()
        {
            LeftTop = color,
            RightTop = color,
            LeftBottom = color,
            RightBottom = color,
        };

    public static implicit operator T(TextureUV<T> uv)
    {
        if (
            EqualityComparer<T>.Default.Equals(uv.LeftTop, uv.RightTop)
            && EqualityComparer<T>.Default.Equals(uv.LeftTop, uv.LeftBottom)
            && EqualityComparer<T>.Default.Equals(uv.LeftTop, uv.RightBottom)
        )
            return uv.LeftTop;
        throw new InvalidCastException("UV 四个角点的值不同, 无法转换为单一值");
    }
}

/// <summary>
/// 实体纹理组件
/// </summary>
[Component]
public struct Sprite()
{
    /// <summary>
    /// 精灵纹理
    /// </summary>
    public TextureRegion? Texture = null;

    /// <summary>
    /// 纹理的过渡
    /// </summary>
    public TextureUV<float> Gradient = 1.0f;

    /// <summary>
    /// 精灵的掩膜颜色
    /// </summary>
    public Color Color = Color.White;

    /// <summary>
    /// 精灵的透明度
    /// </summary>
    public float Alpha = 1.0f;

    /// <summary>
    /// 纹理逻辑边框在世界中的尺寸
    /// </summary>
    public Vector2 Size = Vector2.Zero;

    /// <summary>
    /// 精灵逻辑原点相对实体的坐标
    /// </summary>
    public Vector2 Position = Vector2.Zero;

    /// <summary>
    /// 精灵相对实体的旋转
    /// </summary>
    public float Rotation = 0;

    /// <summary>
    /// 精灵的缩放
    /// </summary>
    public Vector2 Scale = Vector2.One;

    /// <summary>
    /// 精灵纹理的混合模式
    /// </summary>
    public SpriteBlend Blend = SpriteBlend.Alpha;

    /// <summary>
    /// 是否为平面纹理
    /// </summary>
    public bool Billboard = true;
}

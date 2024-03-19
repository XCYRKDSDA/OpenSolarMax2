using Microsoft.Xna.Framework;
using Nine.Graphics;
using OpenSolarMax.Game.ECS;

namespace OpenSolarMax.Mods.Core.Components;

public enum SpriteBlend
{
    Alpha,
    Additive,
    Opaque,
    NonPremultiplied,
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
    /// 精灵的掩膜颜色
    /// </summary>
    public Color Color = Color.White;

    /// <summary>
    /// 精灵的透明度
    /// </summary>
    public float Alpha = 1.0f;

    /// <summary>
    /// 精灵纹理的锚点在纹理图片坐标系中的相对位置
    /// </summary>
    public Vector2 Anchor = Vector2.Zero;

    /// <summary>
    /// 精灵相对实体的坐标
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
}

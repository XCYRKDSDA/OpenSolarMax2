using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Game.UI;

/// <summary>
/// 滚动背景的控制模式
/// </summary>
internal enum ScrollingBackgroundControlMode
{
    /// <summary>
    /// 自动模式：按线性控制律使视口锚点收敛到视口目标点
    /// </summary>
    Auto,

    /// <summary>
    /// 手动模式：关闭控制律，视口锚点永远立即等于视口目标点
    /// </summary>
    Manual,
}

/// <summary>
/// 水平滚动背景。纹理按视口高度缩放，水平铺满当前整个视口。
/// 用户可设置纹理锚点和视口目标点，组件使用线性控制律将纹理锚点在视口中的位置拉向视口目标点。
/// 当视口尺寸改变时，组件保证视口锚点的坐标值不发生变化。
/// </summary>
internal class HorizontalScrollingBackground(GraphicsDevice graphicsDevice)
{
    private float _textureAnchor;

    // 上一次绘制使用的缩放比，写入纹理锚点时用它同步修正视口锚点；首次绘制前为 0，修正不产生效果
    private float _lastRenderScale;

    /// <summary>
    /// 控制模式，默认自动
    /// </summary>
    public ScrollingBackgroundControlMode ControlMode { get; set; } =
        ScrollingBackgroundControlMode.Auto;

    /// <summary>
    /// 纹理锚点，背景纹理自身坐标系下的水平坐标点，以纹理像素计，0 为纹理左边沿。
    /// 写入时将同步修改视口锚点，使图像位置保持不变
    /// </summary>
    public float TextureAnchor
    {
        get => _textureAnchor;
        set
        {
            ActualAnchorInViewport += (value - _textureAnchor) * _lastRenderScale;
            _textureAnchor = value;
        }
    }

    /// <summary>
    /// 视口目标点，希望纹理锚点落在视口中的水平位置，以画面像素计，相对视口中心度量，0 为视口中心
    /// </summary>
    public float TargetAnchorInViewport { get; set; }

    /// <summary>
    /// 视口锚点，纹理锚点实际落在视口中的水平位置，以画面像素计，相对视口中心度量，0 即视口中心
    /// </summary>
    public float ActualAnchorInViewport { get; set; }

    /// <summary>
    /// 线性控制律中速度与偏差之比，单位 1/秒
    /// </summary>
    public float ConvergenceRate { get; set; } = 5;

    private readonly SpriteBatch _spriteBatch = new(graphicsDevice, 1);

    public Texture2D? Texture { get; set; }

    public float Opacity { get; set; } = 1;

    public void Draw(GameTime gameTime)
    {
        if (Texture is not { } texture)
            return;

        var viewport = graphicsDevice.Viewport;

        // 只支持横向滚动，图片按高度缩放
        var scale = (float)viewport.Height / texture.Height;
        _lastRenderScale = scale;

        // 自动模式按线性控制律收敛，手动模式视口锚点直接取视口目标点
        if (ControlMode == ScrollingBackgroundControlMode.Auto)
        {
            var deviation = TargetAnchorInViewport - ActualAnchorInViewport;
            if (deviation != 0)
            {
                // 本帧走完偏差的比例，最多为 1：最快一步到达目标，不会冲过头
                var step = MathF.Min(
                    ConvergenceRate * (float)gameTime.ElapsedGameTime.TotalSeconds,
                    1f
                );
                ActualAnchorInViewport += step * deviation;
            }
        }
        else if (ControlMode == ScrollingBackgroundControlMode.Manual)
            ActualAnchorInViewport = TargetAnchorInViewport;
        else
            throw new IndexOutOfRangeException("wtf");

        var tileWidth = texture.Width * scale;

        // 把纹理锚点摆到视口锚点的位置，得到纹理左边沿的偏移
        var textureLeft = viewport.Width / 2f + ActualAnchorInViewport - _textureAnchor * scale;
        // 平移整数个纹理宽度，把绘制起点挪到视口左界以左
        var firstLeft = textureLeft - tileWidth * MathF.Ceiling(textureLeft / tileWidth);

        // 计算宽度要绘制多少个背景
        var times = (int)MathF.Ceiling((viewport.Width - firstLeft) / tileWidth);

        // 构造 sourceRectangle，配合 LinearWrap 采样让同一纹理横向重复 times 次
        var sourceRegion = new Rectangle(
            Point.Zero,
            new Point(texture.Width * times, texture.Height)
        );

        _spriteBatch.Begin(samplerState: SamplerState.LinearWrap);
        _spriteBatch.Draw(
            texture,
            new Vector2(firstLeft, 0),
            sourceRegion,
            Color.White * Opacity,
            rotation: 0f,
            origin: Vector2.Zero,
            scale: scale,
            effects: SpriteEffects.None,
            layerDepth: 0
        );
        _spriteBatch.End();
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenSolarMax.Game.UI;

internal class HorizontalScrollingBackground(GraphicsDevice graphicsDevice)
{
    private readonly SpriteBatch _spriteBatch = new(graphicsDevice, 1);

    public Texture2D? Texture { get; set; }

    public float Left { get; set; } = 0;

    public float Alpha { get; set; } = 1;

    public void Draw()
    {
        if (Texture is null) return;

        // 只支持横向滚动，图片按高度缩放
        var scale = (float)graphicsDevice.Viewport.Height / Texture.Height;

        // 计算绘制偏移
        var actualLeft = Left;
        while (actualLeft > 0)
            actualLeft -= Texture.Width * scale;
        while (actualLeft < -Texture.Width * scale)
            actualLeft += Texture.Width * scale;

        // 计算宽度要绘制多少个背景
        var times = (int)MathF.Ceiling((graphicsDevice.Viewport.Width - actualLeft) / (Texture.Width * scale));

        // 构造 sourceRectangle
        var sourceRegion = new Rectangle(Point.Zero, new Point(Texture.Width * times, Texture.Height));

        // 绘图
        _spriteBatch.Begin(samplerState: SamplerState.LinearWrap);
        _spriteBatch.Draw(Texture, new Vector2(actualLeft, 0), sourceRegion, Color.White * Alpha,
                          rotation: 0f, origin: Vector2.Zero, scale: scale,
                          effects: SpriteEffects.None, layerDepth: 0);
        _spriteBatch.End();
    }
}

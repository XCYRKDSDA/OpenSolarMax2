using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;

namespace OpenSolarMax.Game.UI;

internal class WorldRenderer(World world, AggregateSystem renderer, GraphicsDevice graphicsDevice)
    : IFadableImage
{
    private const float _alpha = 0.5f;

    public Point Size => new(-1); // 尽量放大

    public void Draw(RenderContext context, Rectangle dest, Color color, float fadeIn)
    {
        // 预览区域经控件变换后的屏幕位置
        var rect = context.Transform.Apply(dest);

        // 整体不透明度: 控件颜色 × 上下文不透明度 × 淡入进度 × 预览统一不透明度
        var opacity = color.A / 255f * context.Opacity * fadeIn * _alpha;

        // 先落盘已排队的界面绘制, 避免与世界的绘制交错
        context.Flush();

        // 保存裁剪矩形, 并限制到预览区域
        var scissorCache = graphicsDevice.ScissorRectangle;
        var scissor = Rectangle.Intersect(
            scissorCache,
            new Rectangle(
                rect.X + graphicsDevice.Viewport.X,
                rect.Y + graphicsDevice.Viewport.Y,
                rect.Width,
                rect.Height
            )
        );
        graphicsDevice.ScissorRectangle = scissor;

        // 绕过画布, 把世界绘制到当前渲染目标(背景已在其中),
        // 把预览区域写入 Viewport 组件
        world.Query(
            new QueryDescription().WithAll<Viewport, RenderSettings>(),
            (ref Viewport viewport, ref RenderSettings renderSettings) =>
            {
                viewport = new Viewport(rect);
                renderSettings.SpriteScaling = fadeIn;
                renderSettings.Opacity = opacity;
                renderSettings.ScissorTestEnable = true;
            }
        );
        renderer.Update(new GameTime()); // 绘图系统无所谓时间，此处就随便传一个好了

        // 恢复裁剪矩形, 并让界面批处理重新建立自身状态
        graphicsDevice.ScissorRectangle = scissorCache;
        context.Flush();
    }
}

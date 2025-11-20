using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using OpenSolarMax.Game.Modding;

namespace OpenSolarMax.Game.UI;

internal class WorldRenderer(World world, DualStageAggregateSystem renderer, GraphicsDevice graphicsDevice)
    : IFadableImage
{
    // 按照全屏分辨率渲染以避免放大时模糊
    private readonly RenderTarget2D _renderTarget = new(
        graphicsDevice,
        graphicsDevice.PresentationParameters.BackBufferWidth,
        graphicsDevice.PresentationParameters.BackBufferHeight
    );

    public Point Size => new(graphicsDevice.PresentationParameters.BackBufferWidth,
                             graphicsDevice.PresentationParameters.BackBufferHeight);

    public void Draw(RenderContext context, Rectangle dest, Color color, float fadeIn)
    {
        // 渲染到 RenderTarget2D
        var renderTargetsCache = graphicsDevice.GetRenderTargets();
        graphicsDevice.SetRenderTarget(_renderTarget);

        graphicsDevice.Clear(Color.Transparent);
        world.Query(new QueryDescription().WithAll<Viewport>(), (ref Viewport vp) => vp = graphicsDevice.Viewport);
        renderer.Update(new GameTime()); // 绘图系统无所谓时间，此处就随便传一个好了

        graphicsDevice.SetRenderTargets(renderTargetsCache);

        // 绘制到 UI
        context.Draw(_renderTarget, dest, color);
    }
}

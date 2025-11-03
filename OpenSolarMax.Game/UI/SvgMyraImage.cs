using System.Drawing.Imaging;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D;
using Svg;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace OpenSolarMax.Game.UI;

public class SvgMyraImage : IImage
{
    public Point Size { get; }

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SvgDocument _doc;
    private readonly Dictionary<int, Texture2D> _cache = [];
    private readonly Dictionary<int, Task<Texture2D>> _renderTasks = [];

    private Texture2D Render(int logScale)
    {
        var bitmap = _doc.Draw((int)(_doc.ViewBox.Width * (2 << logScale)),
                               (int)(_doc.ViewBox.Height * (2 << logScale)));

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Seek(0, SeekOrigin.Begin);
        return Texture2D.FromStream(_graphicsDevice, stream, DefaultColorProcessors.PremultiplyAlpha);
    }

    public SvgMyraImage(GraphicsDevice graphicsDevice, SvgDocument doc)
    {
        _graphicsDevice = graphicsDevice;
        _doc = doc;

        // 尺寸设置为等比例的无限大，以尽可能放大图片
        Size = doc.ViewBox.Width < doc.ViewBox.Height
                   ? new Point((int)(int.MaxValue * doc.ViewBox.Width / doc.ViewBox.Height), int.MaxValue)
                   : new Point(int.MaxValue, (int)(int.MaxValue * doc.ViewBox.Height / doc.ViewBox.Width));

        // 绘制默认大小作为首个缓存
        _cache.Add(0, Render(0));
    }

    public void Draw(RenderContext context, Rectangle dest, Color color)
    {
        var scale = MathF.Max(dest.Width / _doc.ViewBox.Width, dest.Height / _doc.ViewBox.Height);
        var logScale = (int)MathF.Ceiling(MathF.Log2(scale));

        if (!_cache.TryGetValue(logScale, out var texture))
        {
            if (!_renderTasks.TryGetValue(logScale, out var task))
            {
                // 无对应任务，启动后台渲染
                _renderTasks.Add(logScale, Task.Run(() => Render(logScale)));
                // 取最接近的纹理先渲染着
                texture = _cache.OrderBy(p => MathF.Abs(p.Key - logScale)).First().Value;
            }
            else if (!task.IsCompleted)
            {
                // 任务已启动，但还未完成。取最接近的纹理
                texture = _cache.OrderBy(p => MathF.Abs(p.Key - logScale)).First().Value;
            }
            else
            {
                // 任务已完成，获取并记录结果
                texture = task.Result;
                _cache.Add(logScale, texture);
                _renderTasks.Remove(logScale);
            }
        }

        context.Draw(texture, dest, color);
    }
}

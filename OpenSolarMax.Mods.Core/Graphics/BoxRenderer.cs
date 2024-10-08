﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;

namespace OpenSolarMax.Mods.Core.Graphics;

/// <summary>
/// 直接使用<see cref="Microsoft.Xna.Framework.Graphics.GraphicsDevice"/>、
/// 并使用内置效果Box.fx来绘制方框的渲染器
/// </summary>
/// <param name="graphicsDevice"></param>
/// <param name="assets"></param>
internal class BoxRenderer(GraphicsDevice graphicsDevice, IAssetsManager assets)
{
    private readonly VertexPositionColor[] _vertices = new VertexPositionColor[4];
    private static readonly int[] _indices = [0, 1, 2, 3];

    public BoxEffect Effect { get; } = new(graphicsDevice, assets);

    public GraphicsDevice GraphicsDevice => graphicsDevice;

    public void DrawBox(Rectangle rectangle, Color color, float thickness)
    {
        Effect.Shape = rectangle;
        Effect.Thickness = thickness;

        var halfThickness = thickness / 2;
        var min = rectangle.Location;
        var max = rectangle.Location + rectangle.Size;
        _vertices[0].Position = new(min.X - halfThickness, max.Y + halfThickness, 0);
        _vertices[1].Position = new(max.X + halfThickness, max.Y + halfThickness, 0);
        _vertices[2].Position = new(min.X - halfThickness, min.Y - halfThickness, 0);
        _vertices[3].Position = new(max.X + halfThickness, min.Y - halfThickness, 0);
        _vertices[0].Color = _vertices[1].Color = _vertices[2].Color = _vertices[3].Color = color;

        foreach (var pass in Effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleStrip, _vertices, 0, 4, _indices, 0, 2);
        }
    }
}

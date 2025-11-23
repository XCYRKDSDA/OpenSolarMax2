using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[PreviewSystem, AfterStructuralChanges]
[ReadCurr(typeof(Camera)), ReadCurr(typeof(Shape)), ReadCurr(typeof(AbsoluteTransform))]
[Priority((int)GraphicsLayer.Entities)]
public sealed partial class DrawShapesSystem(World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    : ICalcSystem
{
    private static readonly short[] _indices = [0, 1, 2, 3, 2, 1];

    private static readonly QueryDescription _previewableDesc =
        new QueryDescription().WithAll<Shape, AbsoluteTransform>();

    private readonly Effect _effect = new(graphicsDevice, assets.Load<byte[]>("Effects/Sprite.mgfxo"));
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[4];

    public void Update()
    {
        var drawableEntities = new List<Entity>();
        world.Query(in _previewableDesc, entity => drawableEntities.Add(entity));
        drawableEntities.Sort((l, r) => Comparer<float>.Default.Compare(l.Get<AbsoluteTransform>().Translation.Z,
                                                                        r.Get<AbsoluteTransform>().Translation.Z));

        RenderToCameraQuery(world, drawableEntities);
    }

    private void DrawEntity(in Shape shape, in AbsoluteTransform absoluteTransform, in PreviewStatus previewStatus)
    {
        if (shape.Texture is null)
            return;

        // 计算精灵纹理锚点到世界的变换
        var anchorToWorld = Matrix.CreateRotationZ(shape.Rotation)
                            * Matrix.CreateTranslation(shape.Position.X, shape.Position.Y, 0)
                            * absoluteTransform.TransformToRoot;

        // 将变换投影到二维平面
        var rotatedUnitX = Vector3.Transform(Vector3.UnitX, absoluteTransform.Rotation);
        anchorToWorld = Matrix.CreateRotationZ(MathF.Atan2(rotatedUnitX.Y, rotatedUnitX.X))
                        * Matrix.CreateTranslation(anchorToWorld.Translation);

        // 完成最后的缩放
        anchorToWorld = Matrix.CreateScale(shape.Scale.X * shape.Size.X / shape.Texture.LogicalSize.X,
                                           shape.Scale.Y * shape.Size.Y / shape.Texture.LogicalSize.Y, 1)
                        * Matrix.CreateScale(previewStatus.Scale, previewStatus.Scale, 1)
                        * anchorToWorld;

        var leftTop = new Vector3(-shape.Texture.LogicalOrigin.X, shape.Texture.LogicalOrigin.Y, 0);
        var leftToRight = new Vector3(shape.Texture.Bounds.Width, 0, 0);
        var topToBottom = new Vector3(0, -shape.Texture.Bounds.Height, 0);

        // 计算四个顶点的坐标
        _vertices[0].Position = Vector3.Transform(leftTop, anchorToWorld);
        _vertices[1].Position = Vector3.Transform(leftTop + leftToRight, anchorToWorld);
        _vertices[2].Position = Vector3.Transform(leftTop + topToBottom, anchorToWorld);
        _vertices[3].Position = Vector3.Transform(leftTop + leftToRight + topToBottom, anchorToWorld);

        // 计算四个顶点对应的原始纹理的UV坐标
        _vertices[0].TextureCoordinate = new(shape.Texture.Bounds.Left / (float)shape.Texture.Texture.Width,
                                             shape.Texture.Bounds.Top / (float)shape.Texture.Texture.Height);
        _vertices[1].TextureCoordinate = new(shape.Texture.Bounds.Right / (float)shape.Texture.Texture.Width,
                                             shape.Texture.Bounds.Top / (float)shape.Texture.Texture.Height);
        _vertices[2].TextureCoordinate = new(shape.Texture.Bounds.Left / (float)shape.Texture.Texture.Width,
                                             shape.Texture.Bounds.Bottom / (float)shape.Texture.Texture.Height);
        _vertices[3].TextureCoordinate = new(shape.Texture.Bounds.Right / (float)shape.Texture.Texture.Width,
                                             shape.Texture.Bounds.Bottom / (float)shape.Texture.Texture.Height);

        // 设置四个顶点的颜色
        _vertices[0].Color = _vertices[1].Color = _vertices[2].Color = _vertices[3].Color = shape.Color;

        // 设置混合模式
        graphicsDevice.BlendState = BlendState.AlphaBlend;

        // 设置Shader纹理
        _effect.Parameters["tex_sampler+tex"].SetValue(shape.Texture.Texture);

        // 绘制图元
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, 4, _indices, 0, 2);
        }
    }

    [Query]
    [All<Camera, AbsoluteTransform, PreviewStatus>]
    private void RenderToCamera([Data] IEnumerable<Entity> entities, in Camera camera, in AbsoluteTransform pose,
                                in PreviewStatus previewStatus)
    {
        // 计算相机参数
        var view = Matrix.Invert(pose.TransformToRoot);
        var projection = Matrix.CreateOrthographic(camera.Width, camera.Height, camera.ZNear, camera.ZFar);
        _effect.Parameters["to_ndc"].SetValue(view * projection);

        // 设置绘图区域
        var oldViewport = graphicsDevice.Viewport;
        graphicsDevice.Viewport = camera.Output;

        // 设置绘图设备参数
        graphicsDevice.BlendState = BlendState.AlphaBlend;
        graphicsDevice.DepthStencilState = DepthStencilState.None;
        graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 逐个绘制
        foreach (var entity in entities)
        {
            var refs = entity.Get<Shape, AbsoluteTransform>();
            DrawEntity(in refs.t0, in refs.t1, in previewStatus);
        }

        // 恢复 Viewport
        graphicsDevice.Viewport = oldViewport;
    }
}

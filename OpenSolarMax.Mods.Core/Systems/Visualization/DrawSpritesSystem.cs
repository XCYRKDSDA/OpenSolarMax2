using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[DrawSystem]
[ExecuteAfter(typeof(UpdateCameraOutputSystem))]
public sealed partial class DrawSpritesSystem(World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly GraphicsDevice _graphicsDevice = graphicsDevice;
    private readonly VertexPositionColorTexture[] _vertices = new VertexPositionColorTexture[4];
    private static readonly short[] _indices = [0, 1, 2, 3, 2, 1];

    private readonly Effect _effect = new(graphicsDevice, assets.Load<byte[]>("Effects/Tint.mgfxo"));

    private void DrawEntity(in Sprite sprite, in AbsoluteTransform absoluteTransform)
    {
        if (sprite.Texture is null)
            return;

        // 计算精灵纹理锚点到世界的变换
        var anchorToWorld = Matrix.CreateRotationZ(sprite.Rotation)
                            * Matrix.CreateTranslation(sprite.Position.X, sprite.Position.Y, 0)
                            * absoluteTransform.TransformToRoot;

        if (sprite.Billboard)
        {
            // 将变换投影到二维平面
            var rotatedUnitX = Vector3.Transform(Vector3.UnitX, absoluteTransform.Rotation);
            anchorToWorld = Matrix.CreateRotationZ(MathF.Atan2(rotatedUnitX.Y, rotatedUnitX.X))
                            * Matrix.CreateTranslation(anchorToWorld.Translation);
        }

        // 完成最后的缩放
        anchorToWorld = Matrix.CreateScale(sprite.Scale.X, sprite.Scale.Y, 1)
                        * anchorToWorld;

        // 计算四个顶点的坐标
        _vertices[0].Position = Vector3.Transform(new(-sprite.Anchor.X, sprite.Anchor.Y, 0), anchorToWorld);
        _vertices[1].Position = Vector3.Transform(
            new(sprite.Texture.Bounds.Width - sprite.Anchor.X, sprite.Anchor.Y, 0), anchorToWorld);
        _vertices[2].Position = Vector3.Transform(
            new(-sprite.Anchor.X, sprite.Anchor.Y - sprite.Texture.Bounds.Height, 0), anchorToWorld);
        _vertices[3].Position = Vector3.Transform(
            new(sprite.Texture.Bounds.Width - sprite.Anchor.X, sprite.Anchor.Y - sprite.Texture.Bounds.Height, 0),
            anchorToWorld);

        // 计算四个顶点对应的原始纹理的UV坐标
        _vertices[0].TextureCoordinate = new(sprite.Texture.Bounds.Left / (float)sprite.Texture.Texture.Width,
                                             sprite.Texture.Bounds.Top / (float)sprite.Texture.Texture.Height);
        _vertices[1].TextureCoordinate = new(sprite.Texture.Bounds.Right / (float)sprite.Texture.Texture.Width,
                                             sprite.Texture.Bounds.Top / (float)sprite.Texture.Texture.Height);
        _vertices[2].TextureCoordinate = new(sprite.Texture.Bounds.Left / (float)sprite.Texture.Texture.Width,
                                             sprite.Texture.Bounds.Bottom / (float)sprite.Texture.Texture.Height);
        _vertices[3].TextureCoordinate = new(sprite.Texture.Bounds.Right / (float)sprite.Texture.Texture.Width,
                                             sprite.Texture.Bounds.Bottom / (float)sprite.Texture.Texture.Height);

        // 设置四个顶点的颜色
        _vertices[0].Color = _vertices[1].Color = _vertices[2].Color = _vertices[3].Color = sprite.Color * sprite.Alpha;

        // 设置混合模式
        _graphicsDevice.BlendState = sprite.Blend switch
        {
            SpriteBlend.Alpha => BlendState.AlphaBlend,
            SpriteBlend.Additive => BlendState.Additive,
            SpriteBlend.Opaque => BlendState.Opaque,
            SpriteBlend.NonPremultiplied => BlendState.NonPremultiplied,
            _ => throw new ArgumentOutOfRangeException()
        };

        // 设置Shader纹理
        _effect.Parameters["tex_sampler+tex"].SetValue(sprite.Texture.Texture);

        // 绘制图元
        foreach (var pass in _effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            _graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertices, 0, 4, _indices, 0, 2);
        }
    }

    [Query]
    [All<Camera, AbsoluteTransform>]
    private void RenderToCamera([Data] IEnumerable<Entity> entities, in Camera camera, in AbsoluteTransform pose)
    {
        // 计算相机参数
        var view = Matrix.Invert(pose.TransformToRoot);
        var projection = Matrix.CreateOrthographic(camera.Width, camera.Height, camera.ZNear, camera.ZFar);
        _effect.Parameters["to_ndc"].SetValue(view * projection);

        // 设置绘图区域
        _graphicsDevice.Viewport = camera.Output;

        // 设置绘图设备参数
        _graphicsDevice.BlendState = BlendState.AlphaBlend;
        _graphicsDevice.DepthStencilState = DepthStencilState.None;
        _graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        _graphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

        // 逐个绘制
        foreach (var entity in entities)
        {
            var refs = entity.Get<Sprite, AbsoluteTransform>();
            DrawEntity(in refs.t0, in refs.t1);
        }
    }

    private static readonly QueryDescription _drawableDesc
        = new QueryDescription().WithAll<Sprite, AbsoluteTransform>();

    public override void Update(in GameTime t)
    {
        var drawableEntities = new List<Entity>();
        World.GetEntities(in _drawableDesc, drawableEntities);
        drawableEntities.Sort(
            (l, r) => Comparer<float>.Default.Compare(l.Get<AbsoluteTransform>().Translation.Z,
                                                      r.Get<AbsoluteTransform>().Translation.Z)
        );

        RenderToCameraQuery(World, drawableEntities);
    }
}

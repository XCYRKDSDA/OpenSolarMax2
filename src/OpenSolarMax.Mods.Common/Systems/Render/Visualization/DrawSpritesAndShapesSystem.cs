using Apos.Shapes;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Game.Modding.UI;
using OpenSolarMax.Mods.Common.Components;
using OpenSolarMax.Mods.Common.Graphics;
using OpenSolarMax.Mods.Common.Utils;
using Shape = OpenSolarMax.Mods.Common.Components.Shape;

namespace OpenSolarMax.Mods.Common.Systems;

[RenderSystem, LateUpdate, BothForGameplayAndPreview]
[
    ReadCurr(typeof(Projection)),
    ReadCurr(typeof(Sprite)),
    ReadCurr(typeof(Shape)),
    ReadCurr(typeof(AbsoluteTransform))
]
[Priority((int)GraphicsLayer.Entities)]
public sealed partial class DrawSpritesAndShapesSystem(World world, ShapeBatch batch) : ICalcSystem
{
    // 加法混合的状态只建一次。BlendState 是 GraphicsResource，在循环里新建会让它反复被
    // 登记进 GraphicsDevice 的资源表，并在终结时线性查找移除，与主线程抢同一把锁，
    // 造成偶发的长停顿。
    private static readonly BlendState _additiveBlendState = new()
    {
        ColorSourceBlend = Blend.One,
        AlphaSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaDestinationBlend = Blend.One,
    };

    // 启用裁剪测试的光栅化状态
    private static readonly RasterizerState _scissorRasterizerState = new()
    {
        CullMode = CullMode.CullClockwiseFace,
        ScissorTestEnable = true,
    };

    private static readonly QueryDescription _drawableDesc = new QueryDescription()
        .WithAll<AbsoluteTransform>()
        .WithAny<Sprite, Shape>();

    public void Update()
    {
        var drawableEntities = new List<Entity>();
        world.Query(in _drawableDesc, entity => drawableEntities.Add(entity));
        drawableEntities.Sort(
            (l, r) =>
            {
                var order = Comparer<float>.Default.Compare(
                    l.Get<AbsoluteTransform>().Translation.Z,
                    r.Get<AbsoluteTransform>().Translation.Z
                );
                return order != 0 ? order : l.Id.CompareTo(r.Id);
            }
        );

        RenderToCameraQuery(world, drawableEntities);
    }

    private void DrawEntity(
        in Sprite sprite,
        in AbsoluteTransform absoluteTransform,
        in RenderSettings renderSettings
    )
    {
        if (sprite.Texture is null)
            return;

        // 计算精灵纹理锚点到世界的变换
        var anchorToWorld =
            Matrix.CreateRotationZ(sprite.Rotation)
            * Matrix.CreateTranslation(sprite.Position.X, sprite.Position.Y, 0)
            * absoluteTransform.TransformToRoot;

        if (sprite.Billboard)
        {
            // 将变换投影到二维平面
            var projectedRotation = TransformProjection.To2D(
                Quaternion.CreateFromRotationMatrix(anchorToWorld)
            );
            anchorToWorld =
                Matrix.CreateRotationZ(projectedRotation)
                * Matrix.CreateTranslation(anchorToWorld.Translation);
        }

        // 完成最后的缩放
        anchorToWorld =
            Matrix.CreateScale(
                sprite.Scale.X * sprite.Size.X / sprite.Texture.VirtualFrame.Width,
                sprite.Scale.Y * sprite.Size.Y / sprite.Texture.VirtualFrame.Height,
                1
            )
            * Matrix.CreateScale(renderSettings.SpriteScaling, renderSettings.SpriteScaling, 1)
            * anchorToWorld;

        // 计算四边形投影到世界系 XY 平面后的变换
        var leftTop = new Vector3(
            -sprite.Texture.Anchor.X,
            sprite.Texture.Anchor.Y - sprite.Texture.Bounds.Height,
            0
        );
        var origin = Vector3.Transform(leftTop, anchorToWorld);
        var right = Vector3.TransformNormal(
            new Vector3(sprite.Texture.Bounds.Width, 0, 0),
            anchorToWorld
        );
        var down = Vector3.TransformNormal(
            new Vector3(0, sprite.Texture.Bounds.Height, 0),
            anchorToWorld
        );
        var toWorld = new Matrix3x2(right.X, right.Y, down.X, down.Y, origin.X, origin.Y);

        // 以像素为单位选取原始纹理中的子区域
        var bounds = sprite.Texture.Bounds;
        var toSource =
            Matrix3x2.CreateScale(bounds.Width, -bounds.Height)
            * Matrix3x2.CreateTranslation(bounds.Left, bounds.Top + bounds.Height);

        // 设置掩膜颜色
        var mask = sprite.Color * sprite.Alpha * renderSettings.Opacity;

        batch.Draw(sprite.Texture.Texture, toWorld, toSource, mask);
    }

    private void DrawShapeEntity(
        in Shape shape,
        in AbsoluteTransform absoluteTransform,
        in RenderSettings renderSettings
    )
    {
        // 计算形状局部坐标系到世界的变换
        var anchorToWorld = absoluteTransform.TransformToRoot;

        if (shape.Billboard)
        {
            // 将变换投影到二维平面
            var projectedRotation = TransformProjection.To2D(
                Quaternion.CreateFromRotationMatrix(anchorToWorld)
            );
            anchorToWorld =
                Matrix.CreateRotationZ(projectedRotation)
                * Matrix.CreateTranslation(anchorToWorld.Translation);
        }

        // TODO: 这里由于 Apos.Shapes 的接口实际上不支持三维图形，所以 Shape 不管是否 Billboard，都会平行于屏幕来绘制。
        // 但是 Apos.Shapes 的着色器实际上能够支持三维形状，只是 Apos.Shapes 代码接口没有提供这个能力。
        // 因此此处不改变选型结论，后续有需要再自行 fork 修改。

        // 完成最后的缩放
        var scale = shape.Scale * renderSettings.SpriteScaling;
        anchorToWorld = Matrix.CreateScale(scale, scale, 1) * anchorToWorld;

        ShapeBatchRenderer.DrawEntity(
            batch,
            in shape,
            in anchorToWorld,
            scale,
            scale,
            renderSettings.Opacity
        );
    }

    private static BlendState BlendStateOf(SpriteBlend blend) =>
        blend switch
        {
            SpriteBlend.Alpha => BlendState.AlphaBlend,
            SpriteBlend.Additive => _additiveBlendState,
            _ => throw new ArgumentOutOfRangeException(nameof(blend)),
        };

    private void TrySwitchBlendState(
        ref SpriteBlend currentBlend,
        in Matrix worldToNdc,
        SpriteBlend blend,
        RasterizerState rasterizerState
    )
    {
        if (blend == currentBlend)
            return;
        currentBlend = blend;
        batch.End();
        batch.Begin(null, worldToNdc, BlendStateOf(currentBlend), rasterizerState: rasterizerState);
    }

    [Query]
    [All<RenderSettings, Projection>]
    private void RenderToCamera(
        [Data] IEnumerable<Entity> entities,
        in RenderSettings renderSettings,
        in Projection projection
    )
    {
        var currentBlend = SpriteBlend.Alpha;
        var worldToNdc = projection.WorldToNdc;
        var rasterizerState = renderSettings.ScissorTestEnable
            ? _scissorRasterizerState
            : RasterizerState.CullClockwise;
        batch.Begin(null, worldToNdc, BlendStateOf(currentBlend), rasterizerState: rasterizerState);

        foreach (var entity in entities)
        {
            ref readonly var transform = ref entity.Get<AbsoluteTransform>();

            // 同时带有 Sprite 与 Shape 的实体两者都绘制，Sprite 优先
            if (entity.Has<Sprite>())
            {
                ref readonly var sprite = ref entity.Get<Sprite>();
                if (sprite.Texture is not null)
                {
                    TrySwitchBlendState(
                        ref currentBlend,
                        worldToNdc,
                        sprite.Blend,
                        rasterizerState
                    );
                    DrawEntity(in sprite, in transform, in renderSettings);
                }
            }
            if (entity.Has<Shape>())
            {
                ref readonly var shape = ref entity.Get<Shape>();
                TrySwitchBlendState(ref currentBlend, worldToNdc, shape.Blend, rasterizerState);
                DrawShapeEntity(in shape, in transform, in renderSettings);
            }
        }

        batch.End();
    }
}

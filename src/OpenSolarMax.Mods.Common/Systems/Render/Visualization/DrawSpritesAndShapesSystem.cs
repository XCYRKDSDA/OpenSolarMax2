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

namespace OpenSolarMax.Mods.Common.Systems;

[RenderSystem, LateUpdate, BothForGameplayAndPreview]
[ReadCurr(typeof(Projection)), ReadCurr(typeof(Sprite)), ReadCurr(typeof(AbsoluteTransform))]
[Priority((int)GraphicsLayer.Entities)]
public sealed partial class DrawSpritesAndShapesSystem(World world, GraphicsDevice graphicsDevice)
    : ICalcSystem
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

    private static readonly QueryDescription _drawableDesc = new QueryDescription().WithAll<
        Sprite,
        AbsoluteTransform
    >();

    private readonly ShapeBatch _batch = new(graphicsDevice, null, false);

    public void Update()
    {
        var drawableEntities = new List<Entity>();
        world.Query(in _drawableDesc, entity => drawableEntities.Add(entity));
        drawableEntities.Sort(
            (l, r) =>
                Comparer<float>.Default.Compare(
                    l.Get<AbsoluteTransform>().Translation.Z,
                    r.Get<AbsoluteTransform>().Translation.Z
                )
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
        var leftTop = new Vector3(-sprite.Texture.Anchor.X, sprite.Texture.Anchor.Y, 0);
        var origin = Vector3.Transform(leftTop, anchorToWorld);
        var right = Vector3.TransformNormal(
            new Vector3(sprite.Texture.Bounds.Width, 0, 0),
            anchorToWorld
        );
        var down = Vector3.TransformNormal(
            new Vector3(0, -sprite.Texture.Bounds.Height, 0),
            anchorToWorld
        );
        var toWorld = new Matrix3x2(right.X, right.Y, down.X, down.Y, origin.X, origin.Y);

        // 以像素为单位选取原始纹理中的子区域
        var bounds = sprite.Texture.Bounds;
        var toSource =
            Matrix3x2.CreateScale(bounds.Width, bounds.Height)
            * Matrix3x2.CreateTranslation(bounds.Left, bounds.Top);

        // 设置掩膜颜色
        var mask = sprite.Color * sprite.Alpha;

        _batch.Draw(sprite.Texture.Texture, toWorld, toSource, mask);
    }

    private static BlendState BlendStateOf(SpriteBlend blend) =>
        blend switch
        {
            SpriteBlend.Alpha => BlendState.AlphaBlend,
            SpriteBlend.Additive => _additiveBlendState,
            _ => throw new ArgumentOutOfRangeException(nameof(blend)),
        };

    [Query]
    [All<RenderSettings, Projection>]
    private void RenderToCamera(
        [Data] IEnumerable<Entity> entities,
        in RenderSettings renderSettings,
        in Projection projection
    )
    {
        var currentBlend = SpriteBlend.Alpha;
        _batch.Begin(null, projection.WorldToNdc, BlendStateOf(currentBlend));

        foreach (var entity in entities)
        {
            var refs = entity.Get<Sprite, AbsoluteTransform>();
            if (refs.t0.Texture is null)
                continue;

            // 混合模式变化时分段：结束当前批次，以新的混合状态重开
            if (refs.t0.Blend != currentBlend)
            {
                currentBlend = refs.t0.Blend;
                _batch.End();
                _batch.Begin(null, projection.WorldToNdc, BlendStateOf(currentBlend));
            }

            DrawEntity(in refs.t0, in refs.t1, in renderSettings);
        }

        _batch.End();
    }
}

using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[RenderSystem, LateUpdate, BothForGameplayAndPreview]
[ReadCurr(typeof(Camera)), ReadCurr(typeof(Viewport)), ReadCurr(typeof(AbsoluteTransform))]
[Write(typeof(Projection))]
public sealed partial class CalculateCameraProjectionSystem(
    World world,
    GraphicsDevice graphicsDevice
) : ICalcSystem
{
    [Query]
    [All<Camera, Viewport, AbsoluteTransform, Projection>]
    private void Calculate(
        in Camera camera,
        in Viewport viewport,
        in AbsoluteTransform pose,
        ref Projection projection
    )
    {
        var fullViewport = graphicsDevice.Viewport;

        // 计算 letterbox：将 Viewport（关卡边界）按相机纵横比缩放居中
        var scaleX = viewport.Width / camera.Width;
        var scaleY = viewport.Height / camera.Height;
        var scale = MathF.Min(scaleX, scaleY);
        var levelBoundary = new Rectangle(
            viewport.X + (viewport.Width - (int)MathF.Round(scale * camera.Width)) / 2,
            viewport.Y + (viewport.Height - (int)MathF.Round(scale * camera.Height)) / 2,
            (int)MathF.Round(scale * camera.Width),
            (int)MathF.Round(scale * camera.Height)
        );

        // 观测矩阵：将世界坐标变换到相机空间
        var worldToCamera = Matrix.Invert(pose.TransformToRoot);

        // 计算关卡边界四边到渲染目标四边的留白
        var leftMargin = levelBoundary.X - fullViewport.X;
        var rightMargin =
            fullViewport.X + fullViewport.Width - (levelBoundary.X + levelBoundary.Width);
        var topMargin = levelBoundary.Y - fullViewport.Y;
        var bottomMargin =
            fullViewport.Y + fullViewport.Height - (levelBoundary.Y + levelBoundary.Height);

        // 视锥体投影矩阵：按四边留白非对称扩张，使关卡边界外的实体也能落入视锥体
        var cameraToNdc = Matrix.CreateOrthographicOffCenter(
            -camera.Width / 2 - leftMargin * camera.Width / levelBoundary.Width,
            camera.Width / 2 + rightMargin * camera.Width / levelBoundary.Width,
            -camera.Height / 2 - bottomMargin * camera.Height / levelBoundary.Height,
            camera.Height / 2 + topMargin * camera.Height / levelBoundary.Height,
            camera.ZNear,
            camera.ZFar
        );

        // 屏幕到 NDC 的变换：将绝对屏幕坐标映射到 NDC
        var screenToNdc = Matrix.CreateOrthographicOffCenter(
            0,
            fullViewport.Width,
            fullViewport.Height,
            0,
            0,
            -1
        );

        // 世界到 NDC 的变换
        projection.WorldToNdc = worldToCamera * cameraToNdc;

        // 屏幕到 NDC 的变换
        projection.ScreenToNdc = screenToNdc;

        // 世界到屏幕的变换
        projection.WorldToScreen = projection.WorldToNdc * Matrix.Invert(screenToNdc);
    }

    public void Update() => CalculateQuery(world);
}

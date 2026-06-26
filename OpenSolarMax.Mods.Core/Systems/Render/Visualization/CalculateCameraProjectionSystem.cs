using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[RenderSystem, AfterStructuralChanges, BothForGameplayAndPreview]
[ReadCurr(typeof(Camera)), ReadCurr(typeof(AbsoluteTransform)), Write(typeof(Projection))]
public sealed partial class CalculateCameraProjectionSystem(
    World world,
    GraphicsDevice graphicsDevice
) : ICalcSystem
{
    [Query]
    [All<Camera, AbsoluteTransform, Projection>]
    private void Calculate(in Camera camera, in AbsoluteTransform pose, ref Projection projection)
    {
        var canvas = camera.Output.Bounds;
        var fullViewport = graphicsDevice.Viewport;

        // 观测矩阵：将世界坐标变换到相机空间
        var worldToCamera = Matrix.Invert(pose.TransformToRoot);

        // 计算画布四边到渲染目标四边的留白
        var leftMargin = canvas.X - fullViewport.X;
        var rightMargin = fullViewport.X + fullViewport.Width - (canvas.X + canvas.Width);
        var topMargin = canvas.Y - fullViewport.Y;
        var bottomMargin = fullViewport.Y + fullViewport.Height - (canvas.Y + canvas.Height);

        // 视锥体投影矩阵：按四边留白非对称扩张，使画布外的实体也能落入视锥体
        var cameraToNdc = Matrix.CreateOrthographicOffCenter(
            -camera.Width / 2 - leftMargin * camera.Width / canvas.Width,
            camera.Width / 2 + rightMargin * camera.Width / canvas.Width,
            -camera.Height / 2 - bottomMargin * camera.Height / canvas.Height,
            camera.Height / 2 + topMargin * camera.Height / canvas.Height,
            camera.ZNear,
            camera.ZFar
        );

        // 画布到 NDC 的变换：将画布相对坐标映射到 NDC，补偿画布在渲染目标中的偏移
        var canvasToNdc = Matrix.CreateOrthographicOffCenter(
            -canvas.X,
            fullViewport.X + fullViewport.Width - canvas.X,
            fullViewport.Y + fullViewport.Height - canvas.Y,
            -canvas.Y,
            0,
            -1
        );

        // 世界到 NDC 的变换
        projection.WorldToNdc = worldToCamera * cameraToNdc;

        // 画布到 NDC 的变换
        projection.CanvasToNdc = canvasToNdc;

        // 世界到画布的变换
        projection.WorldToCanvas = projection.WorldToNdc * Matrix.Invert(canvasToNdc);
    }

    public void Update() => CalculateQuery(world);
}

using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[RenderSystem, AfterStructuralChanges]
[ReadCurr(typeof(Viewport)), Write(typeof(Camera))]
public sealed partial class UpdateCameraOutputSystem(World world) : ICalcSystem
{
    public void Update() => UpdateOutputQuery(world);

    [Query]
    [All<Camera, Viewport>]
    private static void UpdateOutput(ref Camera camera, in Viewport viewport)
    {
        // 计算缩放
        var scaleX = viewport.Width / camera.Width;
        var scaleY = viewport.Height / camera.Height;
        var scale = MathF.Min(scaleX, scaleY);
        camera.Output.Width = (int)MathF.Round(scale * camera.Width);
        camera.Output.Height = (int)MathF.Round(scale * camera.Height);

        // 居中
        camera.Output.X = viewport.X + (viewport.Width - camera.Output.Width) / 2;
        camera.Output.Y = viewport.Y + (viewport.Height - camera.Output.Height) / 2;
    }
}

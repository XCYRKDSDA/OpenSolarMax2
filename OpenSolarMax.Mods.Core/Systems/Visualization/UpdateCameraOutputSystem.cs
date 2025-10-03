using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[RenderSystem]
[Write(typeof(Camera))]
// LevelUIContext 粒度太粗，此处不写
public sealed partial class UpdateCameraOutputSystem(World world) : ILateUpdateSystem
{
    [Query]
    [All<Camera, LevelUIContext>]
    private static void UpdateOutput(ref Camera camera, in LevelUIContext uiContext)
    {
        var worldBounds = uiContext.WorldPad.ContainerBounds;

        var scaleX = worldBounds.Width / camera.Width;
        var scaleY = worldBounds.Height / camera.Height;
        var scale = MathF.Min(scaleX, scaleY);

        camera.Output = new Viewport(worldBounds.X, worldBounds.Y,
                                     (int)MathF.Round(scale * camera.Width), (int)MathF.Round(scale * camera.Height));
    }

    public void Update() => UpdateOutputQuery(world);
}

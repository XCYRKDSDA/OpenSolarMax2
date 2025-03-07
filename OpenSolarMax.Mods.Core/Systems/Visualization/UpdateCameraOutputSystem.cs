﻿using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[DrawSystem]
public sealed partial class UpdateCameraOutputSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
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
}

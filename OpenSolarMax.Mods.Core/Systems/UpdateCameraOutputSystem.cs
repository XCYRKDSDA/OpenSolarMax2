using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[DrawSystem]
[ExecuteBefore(typeof(DrawSpritesSystem))]
public sealed partial class UpdateCameraOutputSystem(World world, GraphicsDevice graphicsDevice, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<Camera>]
    private static void UpdateOutput(ref Camera camera)
    {
        camera.Output = new(0, 0, 1920, 1080);
    }
}

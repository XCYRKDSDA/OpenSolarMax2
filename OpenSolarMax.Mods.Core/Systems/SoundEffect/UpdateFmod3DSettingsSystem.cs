using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using FMOD;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using FmodSystem = FMOD.Studio.System;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
public sealed partial class UpdateFmod3DSettingsSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<FmodSystem, Camera, LevelUIContext>]
    private static void SetHearer3DAttributes(ref FmodSystem fmodSystem, in Camera camera, in LevelUIContext ui)
    {
        var scale = ui.WorldPad.ActualBounds.Width / 144f * 25.4f / 1000f / camera.Width;

        var flag = fmodSystem.getCoreSystem(out var fmodCoreSystem);
        if (flag != RESULT.OK)
            throw new Exception($"Failed to get core system with result: {flag}");

        fmodCoreSystem.set3DSettings(1, scale, scale);
    }
}

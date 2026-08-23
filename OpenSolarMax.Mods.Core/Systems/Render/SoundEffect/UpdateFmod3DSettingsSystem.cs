using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using FMOD;
using Microsoft.Xna.Framework.Graphics;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;
using FmodSystem = FMOD.Studio.System;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 根据关卡边界和相机视域尺寸，同步 FMOD 核心系统的 3D 设置（听者距离因子）
/// </summary>
[RenderSystem, LateUpdate]
[ReadCurr(typeof(Camera)), ReadCurr(typeof(Viewport)), Write(typeof(FmodSystem))]
public sealed partial class UpdateFmod3DSettingsSystem(World world) : ICalcSystem
{
    [Query]
    [All<FmodSystem, Camera, Viewport>]
    private static void SetHearer3DAttributes(
        ref FmodSystem fmodSystem,
        in Camera camera,
        in Viewport viewport
    )
    {
        var scale = viewport.Width / 144f * 25.4f / 1000f / camera.Width;

        var flag = fmodSystem.getCoreSystem(out var fmodCoreSystem);
        if (flag != RESULT.OK)
            throw new Exception($"Failed to get core system with result: {flag}");

        fmodCoreSystem.set3DSettings(1, scale, scale);
    }

    public void Update() => SetHearer3DAttributesQuery(world);
}

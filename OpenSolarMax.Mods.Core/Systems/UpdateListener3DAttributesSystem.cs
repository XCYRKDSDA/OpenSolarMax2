using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using FmodSystem = FMOD.Studio.System;
using Fmod3DAttributes = FMOD.ATTRIBUTES_3D;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 设置监听器3D属性的系统，
/// 负责将FMOD.Studio.System的位置同步到Fmod体系中
/// </summary>
[LateUpdateSystem]
public sealed partial class UpdateListener3DAttributesSystem(World world, IAssetsManager assets)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<FmodSystem, AbsoluteTransform, Camera>]
    private static void SetHearer3DAttributes(ref FmodSystem fmodSystem, in AbsoluteTransform transform, in Camera camera)
    {
        fmodSystem.setListenerAttributes(0, new Fmod3DAttributes()
        {
            forward = { x = 0, y = 0, z = 1 },
            position =
            {
                x = transform.TransformToRoot.Translation.X,
                y = transform.TransformToRoot.Translation.Y,
                z = camera.Width / 2 / 1.7320508f,
            },
            up = { x = 0, y = 1, z = 0 },
            velocity = { x = 0, y = 0, z = 0 },
        });
    }
}

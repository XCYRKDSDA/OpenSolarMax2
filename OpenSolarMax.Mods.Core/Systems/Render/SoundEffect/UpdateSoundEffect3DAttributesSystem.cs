using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;
using Fmod3DAttributes = FMOD.ATTRIBUTES_3D;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 设置所有音效的3D属性的系统,
/// 负责将音效的位置同步到Fmod体系中
/// </summary>
[RenderSystem, AfterStructuralChanges]
[ReadCurr(typeof(AbsoluteTransform)), Write(typeof(SoundEffect))]
public sealed partial class UpdateSoundEffect3DAttributesSystem(World world) : ICalcSystem
{
    [Query]
    [All<SoundEffect, AbsoluteTransform>]
    private static void Set3DAttributes(ref SoundEffect soundEffect, in AbsoluteTransform transform)
    {
        soundEffect.EventInstance.set3DAttributes(new Fmod3DAttributes()
        {
            forward = { x = 0, y = 0, z = 1 },
            position =
            {
                x = transform.TransformToRoot.Translation.X,
                y = transform.TransformToRoot.Translation.Y,
                z = transform.TransformToRoot.Translation.Z
            },
            up = { x = 0, y = 1, z = 0 },
            velocity = { x = 0, y = 0, z = 0 },
        });
    }

    public void Update() => Set3DAttributesQuery(world);
}

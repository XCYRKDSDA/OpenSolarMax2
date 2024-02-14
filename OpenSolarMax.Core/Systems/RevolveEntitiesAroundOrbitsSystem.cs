using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Core.Components;
using OpenSolarMax.Core.Utils;

namespace OpenSolarMax.Core.Systems;

/// <summary>
/// 处理实体绕其轨道公转的系统
/// </summary>
public sealed partial class RevolveEntitiesAroundOrbitsSystem(World world) : BaseSystem<World, GameTime>(world)
{
    [Query]
    [All<TreeRelationship<RelativeTransform>, RelativeTransform, RevolutionOrbit, RevolutionState>]
    private static void UpdateRevolution([Data] GameTime time, in RevolutionOrbit orbit, ref RevolutionState state, ref RelativeTransform transform)
    {
        // 更新旋转状态
        state.Phase += MathF.PI * 2 * (float)time.ElapsedGameTime.TotalSeconds / orbit.Period;

        // 更新相对位姿
        switch (orbit.Mode)
        {
            case RevolutionMode.TranslationAndRotation:
                transform = Revolution.CalculateTransform(in orbit, in state);
                break;

            case RevolutionMode.TranslationOnly:
                transform.Translation = Revolution.CalculateTransform(in orbit, in state).Translation;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Core.Components;

namespace OpenSolarMax.Core.Systems;

/// <summary>
/// 处理实体绕其轨道公转的系统
/// </summary>
internal sealed partial class RevolveEntitiesAroundOrbitsSystem(World world) : BaseSystem<World, GameTime>(world)
{
    /// <summary>
    /// 计算实体绕其所在轨道公转的相对位姿
    /// </summary>
    /// <param name="orbit">实体所在轨道</param>
    /// <param name="state">实体当前公转状态</param>
    /// <returns>单位相对轨道所在实体的相对变换</returns>
    private static RelativeTransform CalculateRevolutionTransform(in RevolutionOrbit orbit, in RevolutionState state) =>
        // 以+Z轴为轴, 逆时针旋转
        new(Matrix.CreateTranslation(orbit.Shape.Width, 0, 0)
            * Matrix.CreateRotationZ(state.Phase)
            * Matrix.CreateScale(1, orbit.Shape.Height / orbit.Shape.Width, 1)
            * Matrix.CreateFromQuaternion(orbit.Rotation));

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
                transform = CalculateRevolutionTransform(in orbit, in state);
                break;

            case RevolutionMode.TranslationOnly:
                transform.Translation = CalculateRevolutionTransform(in orbit, in state).Translation;
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}

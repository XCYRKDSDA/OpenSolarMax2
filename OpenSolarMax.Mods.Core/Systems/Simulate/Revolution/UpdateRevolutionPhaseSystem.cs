using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 更新公转相位的系统
/// </summary>
[SimulateSystem, BeforeStructuralChanges]
[ReadPrev(typeof(RevolutionOrbit)), Iterate(typeof(RevolutionState))]
[ExecuteBefore(typeof(ApplyAnimationSystem))]
public sealed partial class UpdateRevolutionPhaseSystem(World world) : ITickSystem
{
    [Query]
    [All<RevolutionOrbit, RevolutionState>]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void UpdateRevolution([Data] GameTime time, in RevolutionOrbit orbit, ref RevolutionState state)
    {
        // 更新旋转状态
        state.Phase += MathF.PI * 2 * (float)time.ElapsedGameTime.TotalSeconds / orbit.Period;
    }

    public void Update(GameTime gameTime) => UpdateRevolutionQuery(world, gameTime);
}

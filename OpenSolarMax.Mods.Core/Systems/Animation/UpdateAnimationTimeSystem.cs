using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 更新默认动画组件的播放时间的系统
/// </summary>
[SimulateSystem, Stage1, Write(typeof(Animation))]
public sealed partial class UpdateAnimationTimeSystem(World world) : ISystem
{
    [Query]
    [All<Animation>]
    private static void Animate([Data] GameTime t, ref Animation animation)
    {
        // 当没有指定动画切片时不更新播放时间
        if (animation.Clip is null)
            return;

        animation.TimeElapsed += t.ElapsedGameTime;
    }

    public void Update(GameTime gameTime) => AnimateQuery(world, gameTime);
}

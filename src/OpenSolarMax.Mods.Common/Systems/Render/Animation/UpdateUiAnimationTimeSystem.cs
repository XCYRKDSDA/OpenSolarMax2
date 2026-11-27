using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Ecs;
using OpenSolarMax.Mods.Common.Components;

namespace OpenSolarMax.Mods.Common.Systems;

/// <summary>
/// 更新 UI 动画组件播放时间的系统。按 UI 时间推进，不受仿真速度影响。
/// </summary>
[RenderSystem, Update, Tick(typeof(UiAnimation))]
public sealed partial class UpdateUiAnimationTimeSystem(World world) : ITickSystem
{
    [Query]
    [All<UiAnimation>]
    private static void Animate([Data] GameTime t, ref UiAnimation animation)
    {
        // 当没有指定动画切片时不更新播放时间
        if (animation.Clip is null)
            return;

        animation.TimeElapsed += t.ElapsedGameTime;
    }

    public void Update(GameTime gameTime) => AnimateQuery(world, gameTime);
}

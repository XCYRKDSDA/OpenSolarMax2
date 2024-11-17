using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 更新默认动画组件的播放时间的系统
/// </summary>
[CoreUpdateSystem]
#pragma warning disable CS9113 // 参数未读。
public sealed partial class UpdateAnimationTimeSystem(World world, IAssetsManager assets)
#pragma warning restore CS9113 // 参数未读。
    : BaseSystem<World, GameTime>(world), ISystem
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
}

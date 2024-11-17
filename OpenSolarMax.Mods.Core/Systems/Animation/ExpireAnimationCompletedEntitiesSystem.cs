using Arch.Buffer;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[StructuralChangeSystem]
#pragma warning disable CS9113 // 参数未读。
public sealed partial class ExpireAnimationCompletedEntitiesSystem(World world, IAssetsManager assets)
#pragma warning restore CS9113 // 参数未读。
    : BaseSystem<World, GameTime>(world), ISystem
{
    private readonly CommandBuffer _commandBuffer = new();

    [Query]
    [All<ExpireAfterAnimationCompleted, Animation>]
    private static void ExpireEntities([Data] CommandBuffer commands,
                                       Entity entity, in Animation animation)
    {
        if (animation.RawClip is not null)
            return;
        
        // 没有指定动画剪辑也算是播完了
        if (animation.Clip is null || animation.TimeElapsed.TotalSeconds > animation.Clip.Length)
            commands.Destroy(entity);
    }

    public override void Update(in GameTime d)
    {
        ExpireEntitiesQuery(World, _commandBuffer);
        _commandBuffer.Playback(World);
    }

    public override void Dispose()
    {
        base.Dispose();
        _commandBuffer.Dispose();
    }
}

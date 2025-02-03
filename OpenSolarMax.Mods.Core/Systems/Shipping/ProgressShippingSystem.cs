using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 更新运输任务状态的系统。该系统作用于运输任务的所有阶段
/// </summary>
[CoreUpdateSystem]
#pragma warning disable CS9113 // 参数未读。
public sealed partial class UpdateShipsStateSystem(World world, IAssetsManager assets)
#pragma warning restore CS9113 // 参数未读。
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<ShippingStatus>]
    private static void Proceed([Data] GameTime time, ref ShippingStatus status)
    {
        if (status.State == ShippingState.Idle) return;

        if (status.State == ShippingState.Charging)
            status.Charging.ElapsedTime += (float)time.ElapsedGameTime.TotalSeconds;
        else if (status.State == ShippingState.Travelling)
            status.Travelling.ElapsedTime += (float)time.ElapsedGameTime.TotalSeconds;
        else
            throw new ArgumentOutOfRangeException();
    }
}

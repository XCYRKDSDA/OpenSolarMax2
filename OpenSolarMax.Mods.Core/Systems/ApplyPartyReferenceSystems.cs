using Arch.Core;
using Nine.Assets;
using OpenSolarMax.Game.System;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 将阵营参考颜色设置到属于阵营的实体的系统
/// </summary>
[LateUpdateSystem]
public sealed class ApplyPartyColorSystem(World world, IAssetsManager assets)
    : ApplyPartyReferenceSystem<Sprite, PartyReferenceColor>(world)
{
    protected override void ApplyPartyReferenceImpl(in PartyReferenceColor reference, ref Sprite target)
    {
        target.Color = reference.Value;
    }
}

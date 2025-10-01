using Arch.Core;
using Microsoft.Xna.Framework;
using Nine.Assets;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 将阵营参考颜色设置到属于阵营的实体的系统
/// </summary>
[LateUpdateSystem]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class ApplyPartyColorSystem(World world)
    : ApplyPartyReferenceSystemBase<Sprite, PartyReferenceColor>(world)
{
    protected override void ApplyDefaultValueImpl(ref Sprite target)
    {
        target.Color = Color.White;
    }

    protected override void ApplyPartyReferenceImpl(in PartyReferenceColor reference, ref Sprite target)
    {
        target.Color = reference.Value;
    }
}

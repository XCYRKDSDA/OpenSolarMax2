using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 将阵营参考颜色设置到属于阵营的实体的系统
/// </summary>
[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(InParty.AsAffiliate), withEntities: true)]
[ReadCurr(typeof(PartyReferenceColor)), Write(typeof(Sprite))]
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

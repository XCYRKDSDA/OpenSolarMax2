using Arch.Core;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.Modding.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

/// <summary>
/// 将阵营参考颜色设置到属于阵营的实体的系统
/// </summary>
[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(InParty.AsAffiliate)), ReadCurr(typeof(PartyReferenceColor)), Write(typeof(Shape))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed class ApplyPartyColorToShapeSystem(World world)
    : ApplyPartyReferenceSystemBase<Shape, PartyReferenceColor>(world)
{
    protected override void ApplyDefaultValueImpl(ref Shape target)
    {
        target.Color = Color.White;
    }

    protected override void ApplyPartyReferenceImpl(in PartyReferenceColor reference, ref Shape target)
    {
        target.Color = reference.Value;
    }
}

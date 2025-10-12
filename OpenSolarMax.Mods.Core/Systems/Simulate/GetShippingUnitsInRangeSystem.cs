using System.Diagnostics;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;
using OpenSolarMax.Mods.Core.Utils;

namespace OpenSolarMax.Mods.Core.Systems;

[SimulateSystem, AfterStructuralChanges]
[ReadCurr(typeof(InParty.AsAffiliate)), ReadCurr(typeof(ShippingStatus)), ReadCurr(typeof(AbsoluteTransform))]
[ReadCurr(typeof(AttackRange)), Write(typeof(InAttackRangeShipsRegistry))]
[ExecuteAfter(typeof(ApplyAnimationSystem))]
public sealed partial class GetShippingUnitsInRangeSystem(World world) : ICalcSystem
{
    [Query]
    [All<InParty.AsAffiliate, ShippingStatus, AbsoluteTransform>]
    private static void GetShippingUnitsInRangeForCertainEntity(
        Entity entity, in InParty.AsAffiliate asAffiliate,
        in ShippingStatus shippingStatus, in AbsoluteTransform unitPose,
        [Data] in AbsoluteTransform planetPose, [Data] in float range,
        [Data] in Registry<Entity, (Entity Ship, float Distance)> result)
    {
        if (shippingStatus.State == ShippingState.Idle)
            return;

        // 矩形判断，减少计算量
        var diffX = unitPose.Translation.X - planetPose.Translation.X;
        var diffY = unitPose.Translation.Y - planetPose.Translation.Y;
        if (diffX > range || diffX < -range || diffY > range || diffY < -range)
            return;

        // 距离判断
        var distance = MathF.Sqrt(diffX * diffX + diffY * diffY);
        if (distance > range)
            return;

        Debug.Assert(asAffiliate.Relationship is not null);
        result[asAffiliate.Relationship.Value.Copy.Party].Add((entity, distance));
    }

    [Query]
    [All<InAttackRangeShipsRegistry, AttackRange, AbsoluteTransform>]
    private void GetShippingUnitsInRange(ref InAttackRangeShipsRegistry registry,
                                         in AttackRange attackRange, in AbsoluteTransform pose)
    {
        foreach (var (_, pairs) in registry.Ships) pairs.Clear();
        GetShippingUnitsInRangeForCertainEntityQuery(world, in pose, in attackRange.Range, registry.Ships);
        foreach (var (_, pairs) in registry.Ships) pairs.Sort((p1, p2) => p1.Item2.CompareTo(p2.Item2));
    }

    public void Update() => GetShippingUnitsInRangeQuery(world);
}

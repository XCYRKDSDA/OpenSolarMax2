using System.Diagnostics;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Xna.Framework;
using OpenSolarMax.Game.ECS;
using OpenSolarMax.Mods.Core.Components;

namespace OpenSolarMax.Mods.Core.Systems;

[LateUpdateSystem]
[ExecuteAfter(typeof(CalculateShipPositionSystem))]
[ExecuteAfter(typeof(CalculateAbsoluteTransformSystem))]
[ExecuteAfter(typeof(IndexPartyAffiliationSystem))]
public sealed partial class GetShippingUnitsInRangeSystem(World world)
    : BaseSystem<World, GameTime>(world), ISystem
{
    [Query]
    [All<InParty.AsAffiliate, ShippingStatus, AbsoluteTransform>]
    private static void GetShippingUnitsInRange(
        Entity entity, in InParty.AsAffiliate asAffiliate,
        in ShippingStatus shippingStatus, in AbsoluteTransform unitPose,
        [Data] in AbsoluteTransform planetPose, [Data] in float range,
        [Data] in Dictionary<EntityReference, List<(EntityReference, float)>> result)
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
        if (result.TryGetValue(asAffiliate.Relationship.Value.Copy.Party, out var list))
            list.Add((entity.Reference(), distance));
        else
            result.Add(asAffiliate.Relationship.Value.Copy.Party, [(entity.Reference(), distance)]);
    }

    private readonly Dictionary<EntityReference, List<(EntityReference, float)>> _cache = [];

    [Query]
    [All<InAttackRangeShipsRegistry, AttackRange, AbsoluteTransform>]
    private void GetShippingUnitsInRange2(ref InAttackRangeShipsRegistry registry,
                                          in AttackRange attackRange, in AbsoluteTransform pose)
    {
        foreach (var list in _cache.Values) list.Clear();
        GetShippingUnitsInRangeQuery(World, in pose, in attackRange.Range, _cache);
        foreach (var list in _cache.Values) list.Sort((p1, p2) => p1.Item2.CompareTo(p2.Item2));

        // 先清空，再填入
        foreach (var list in registry.Ships.Values) list.Clear();
        foreach (var (party, ships) in _cache)
        {
            if (registry.Ships.TryGetValue(party, out var list))
                list.AddRange(ships.Select(p => p.Item1));
            else
                registry.Ships.Add(party, ships.Select(p => p.Item1).ToList());
        }
    }

    public override void Update(in GameTime t)
    {
        GetShippingUnitsInRange2Query(World);
    }
}
